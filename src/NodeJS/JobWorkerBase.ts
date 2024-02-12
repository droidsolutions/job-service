import { add, addDays, sub } from "date-fns";
import { nanoid } from "nanoid";
import type { IJob } from "./Generated/IJob";
import type { IJobRepository } from "./Generated/IJobRepository";
import { JobState } from "./Generated/JobState";
import type { IJobWorkerBase } from "./Generated/Worker/IJobWorkerBase";
import { IJobWorkerMetrics } from "./Generated/Worker/IJobWorkerMetrics";
import type { IJobWorkerSettings } from "./Generated/Worker/Settings/IJobWorkerSettings";
import type { LoggerFactory, SimpleLogger } from "./Helper/LoggerFactory";
import { EmtpyLogger } from "./Helper/LoggerFactory";

/**
 * A base class that allows to implement a job worker.
 * @template TParams The type of the parameter a job has.
 * @template TResult The type of the result of a job.
 */
export abstract class JobWorkerBase<TParams, TResult> implements IJobWorkerBase<TParams, TResult> {
  private currentJob: IJob<TParams, TResult> | undefined;
  private baseLogger: SimpleLogger;
  private cancellationToken: AbortSignal;
  private executedJobs = 0;
  private lastJobDurationMs = 0;
  private lastJobFinishTime: Date | undefined;
  private lastJobDeleteTime: Date | undefined;

  /**
   * Initializes a new instance of the @see JobWorkerBase class.
   * @param {IJobWorkerSettings} settings The job worker settings.
   * @param {IJobRepository<TParams, TResult>} jobRepo An instance of the job repository.
   * @param {LoggerFactory} loggerFactory A factory function that creates an instance of a logger.
   */
  constructor(
    protected settings: IJobWorkerSettings,
    protected jobRepo: IJobRepository<TParams, TResult>,
    loggerFactory?: LoggerFactory,
  ) {
    if (!loggerFactory) {
      loggerFactory = (_, __): SimpleLogger => new EmtpyLogger();
    }
    this.baseLogger = loggerFactory(this.constructor, {
      module: "@droidsolutions-oss/job-service",
    });

    this.assertRepo(this.jobRepo);
  }

  /** The name of this worker as used in logs and written to the job. */
  public runnerName: Readonly<string>;

  /**
   * Returns metrics of this worker instance.
   * @returns {Record<string, unknown>} An object with executedJobs and lastJobDurationMs.
   */
  public getMetrics(): IJobWorkerMetrics {
    let totalSeconds = 0;

    if (this.settings.addNextJobAfter?.days) {
      totalSeconds += this.settings.addNextJobAfter.days * 24 * 60 * 60;
    }

    if (this.settings.addNextJobAfter?.hours) {
      totalSeconds += this.settings.addNextJobAfter.hours * 60 * 60;
    }

    if (this.settings.addNextJobAfter?.minutes) {
      totalSeconds += this.settings.addNextJobAfter.minutes * 60;
    }

    if (this.settings.addNextJobAfter?.seconds) {
      totalSeconds += this.settings.addNextJobAfter.seconds;
    }

    return {
      jobType: this.settings.jobType,
      executedJobs: this.executedJobs,
      lastJobDurationMs: this.lastJobDurationMs,
      lastJobFinishedAt: this.lastJobFinishTime,
      jobIntervallSeconds: this.settings.addNextJobAfter ? totalSeconds : undefined,
    };
  }

  /**
   * Should return a name that distinguishes this worker from other workers. Should be the same for all instances since an id is generated
   * an appended to the string given here.
   */
  public abstract getRunnerName(): string;

  /**
   * Should return the parameters for the initial job at startup. Only needed if an initial job should be created. Can return undefined if
   * no intiial job is needed or job has no parameters.
   */
  public abstract getInitialJobParameters(): TParams | undefined;

  /**
   * A hook that is called before a worker runs. Thisi s called before the job is fetched so it might be that no job is
   * available.
   */
  public abstract preJobRunHook(): void;

  /**
   * A hook that is called after the job is finished Is also called when no job was available.
   */
  public abstract postJobRunHook(): void;

  /**
   * Should contain the logic needed to handle the job. The promise should resolve to the result of the job.
   * @param job The job including its paramters.
   * @param cancellationToken A token to cancel the operation.
   * @returns {Promise<TResult>} A promise that contains the result of the job.
   */
  public abstract processJobAsync(job: IJob<TParams, TResult>, cancellationToken: AbortSignal): Promise<TResult>;

  /**
   * Starts the worker. After a configurable initial delay it will frequently poll for new jobs and take the one with
   * the oldest due date. Each runs calls @see preJobRunHook @see processJobAsync and @see postJobRunHook . If no job is
   * available @see processJobAsync is not called.
   * To stop the worker use the provided stoppingToken.
   * @param {AbortSignal} stoppingToken A token that can be used to stop the worker.
   */
  public async executeAsync(stoppingToken: AbortSignal): Promise<void> {
    this.cancellationToken = stoppingToken;
    await this.delay(this.settings.initialDelaySeconds, stoppingToken);

    const runnerId = nanoid(7);
    this.runnerName = `${this.getRunnerName()}-${runnerId}`;
    let firstRun = true;
    this.baseLogger.info(
      { runner: this.runnerName },
      "Starting runner %s watching for jobs with type %s with polling interval %ds.",
      this.runnerName,
      this.settings.jobType,
      this.settings.jobPollingIntervalSeconds,
    );

    while (!stoppingToken.aborted) {
      const executed = false;
      const startTime = process.hrtime.bigint();
      try {
        this.preJobRunHook();
        await this.handleJobRunAsync(this.settings, stoppingToken, firstRun);
        this.postJobRunHook();

        firstRun = false;

        stoppingToken.throwIfAborted();
        await this.delay(this.settings.jobPollingIntervalSeconds, stoppingToken);
      } catch (err) {
        if (stoppingToken.aborted) {
          this.baseLogger.info({ runner: this.runnerName }, "Stopped runner %s.", this.runnerName);
          break;
        }

        throw err;
      } finally {
        if (executed) {
          const endTime = process.hrtime.bigint();
          this.lastJobDurationMs = Number((endTime - startTime) / 1000000n);
        }

        this.lastJobFinishTime = new Date();
      }
    }
  }

  /**
   * A wrapper around the repository to help set the amount of items the job must process.
   * @param {number} items The amount of items of the job.
   */
  public async setTotalItemsAsync(items: number): Promise<void> {
    this.assertJob(this.currentJob);

    await this.jobRepo.setTotalItemsAsync(this.currentJob, items, this.cancellationToken);
  }

  /**
   * A wrapper around the repository to help add progress to the job.
   * @param {number} amount The amount of progress to add.
   */
  public async addProgressAsync(amount?: number): Promise<void> {
    this.assertJob(this.currentJob);

    await this.jobRepo.addProgressAsync(this.currentJob, amount, false, this.cancellationToken);
  }

  /**
   * A wrapper around the repository to help add failed progress to the job.
   * @param {number} amount The amount of failed progress to add.
   */
  public async addFailedProgressAsync(amount?: number): Promise<void> {
    this.assertJob(this.currentJob);

    await this.jobRepo.addProgressAsync(this.currentJob, amount, true, this.cancellationToken);
  }

  private assertRepo(repo: IJobRepository<TParams, TResult> | undefined): asserts repo {
    if (!repo) {
      throw new Error("Unable to perform action on job because job repository is not set.");
    }
  }

  private assertJob(job: IJob<TParams, TResult> | undefined): asserts job {
    if (!job) {
      throw new Error("Unable to perform action on job because no current job is set.");
    }
  }

  /**
   * A helper function that waits a given amount of time that can be cancelled with a token.
   * @param {number} seconds The amount of seconds to wait.
   * @param {AbortSignal} cancellationToken A token to cancel the waiting.
   * @returns {Promise<void>} A promise that resolves after the given amount of seconds or rejects when the token is cancelled.
   */
  private async delay(seconds: number, cancellationToken: AbortSignal): Promise<void> {
    return new Promise((resolve, reject) => {
      const abortHandler = (_event: Event) => {
        clearTimeout(timeout);
        reject(cancellationToken.reason);
      };

      cancellationToken.addEventListener("abort", abortHandler);
      const timeout = setTimeout(() => {
        cancellationToken.removeEventListener("abort", abortHandler);
        resolve();
      }, seconds * 1000);
    });
  }

  /**
   * Internal handler for job runs. Is called between pre and post hook for every run through the main loop.
   * @param {IJobWorkerSettings} settings The worker settings.
   * @param {AbortSignal} cancellationToken The cancellation token.
   * @param {boolean} firstRun If this is the first run.
   * @returns {Promise<boolean>} A promise that resolves to true when a job was executed or false if no job was found or it was resetted.
   */
  private async handleJobRunAsync(settings: IJobWorkerSettings, cancellationToken: AbortSignal, firstRun?: boolean): Promise<boolean> {
    let executed = false;
    this.currentJob = await this.getJobAsync(this.jobRepo, settings, cancellationToken);

    if (this.currentJob) {
      try {
        this.currentJob.result = await this.processJobAsync(this.currentJob, cancellationToken);
        await this.jobRepo.finishJobAsync(this.currentJob, settings.addNextJobAfter, cancellationToken);

        this.executedJobs++;
        executed = true;
      } catch (err) {
        this.baseLogger.warn(
          { err, jobId: this.currentJob.id, runner: this.runnerName },
          "Failed to process job %d, job is beeing resetted. Message: %s",
          this.currentJob.id,
          (err as Error).message,
        );

        await this.jobRepo.resetJobAsync(this.currentJob, cancellationToken);

        // re-throw if cancellation error so job loop ends
        if (cancellationToken.aborted) {
          throw err;
        }
      }
    } else if (firstRun && settings.addInitialJob) {
      try {
        await this.addInitialJob(settings, cancellationToken);
      } catch (err) {
        this.baseLogger.fatal({ err }, `Unable to create initial job: ${(err as Error).message}`);
        throw new Error("Unable to create initial job.");
      }
    }

    if (this.settings.deleteJobsOlderThan) {
      await this.deleteOldJobs(this.settings.jobType, this.settings.deleteJobsOlderThan, cancellationToken);
    }

    this.currentJob = undefined;

    return executed;
  }

  /**
   * Internal method to check for available jobs.
   * @param {IJobRepository<TParams, TResult>} repo The repository.
   * @param {IJobWorkerSettings} settings The settings.
   * @param {AbortSignal} cancellationToken The cancellation token.
   * @returns {Promise<IJob<TParams, TResult> | undefined>} The next due job or undefined if no job is found.
   */
  private async getJobAsync(
    repo: IJobRepository<TParams, TResult>,
    settings: IJobWorkerSettings,
    cancellationToken: AbortSignal,
  ): Promise<IJob<TParams, TResult> | undefined> {
    try {
      return await repo.getAndStartFirstPendingJobAsync(settings.jobType, this.runnerName, cancellationToken);
    } catch (err) {
      if (cancellationToken.aborted) {
        // app is probably shutting down, this is handled in the main loop
        throw err;
      }

      this.baseLogger.error({ err }, "Error checking for next job: %s", (err as Error).message);

      throw new Error(`Unable to start a new job: ${(err as Error).message}`);
    }
  }

  private async addInitialJob(settings: IJobWorkerSettings, cancellationToken: AbortSignal) {
    // calculate date until which a job with duedate should exist
    let dueDate = new Date();
    if (settings.addNextJobAfter) {
      // use intervall between jobs as limit
      dueDate = add(dueDate, settings.addNextJobAfter);
    } else {
      // Default to 1 day
      dueDate = addDays(dueDate, 1);
    }

    const params = this.getInitialJobParameters();
    const existingJob = await this.jobRepo.findExistingJobAsync(settings.jobType, dueDate, params, true, cancellationToken);

    if (existingJob) {
      this.baseLogger.info("Found existing job %d due %s, skipping add of initial job.", existingJob.id, existingJob.dueDate.toUTCString());
    } else {
      this.baseLogger.info("Did not find any existing %s job that is due until %s, adding initial job.", settings.jobType, dueDate);

      // create a new job if none exists that is due until the calculated time
      await this.jobRepo.addJobAsync(settings.jobType, dueDate, params, cancellationToken);
    }
  }

  private async deleteOldJobs(
    jobType: string,
    olderThan: { days?: number; hours?: number; minutes?: number; seconds?: number },
    cancellationToken: AbortSignal,
  ) {
    const current = new Date();

    // don't spam delete queries for jobs with short intervals
    if (this.lastJobDeleteTime && sub(current, { hours: 24 }) > this.lastJobDeleteTime) {
      return;
    }

    const deleteBefore = sub(current, olderThan);
    const deleted = await this.jobRepo.deleteJobsAsync(jobType, JobState.Finished, deleteBefore, cancellationToken);
    this.lastJobDeleteTime = current;

    if (deleted > 0) {
      this.baseLogger.info("Deleted %d old jobs of type %s.", deleted, jobType);
    }
  }
}
