using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using DroidSolutions.Oss.JobService.Worker.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService.Worker;

/// <summary>
/// A base worker for recurrent jobs managed in a database.
/// </summary>
/// <typeparam name="TParams">The type of the parametersa job can have.</typeparam>
/// <typeparam name="TResult">The type of the result a job can have.</typeparam>
[TsInterface]
[TsAddTypeImport(importTarget: "CancellationToken", importSource: "cancellationtoken")]
public abstract class JobWorkerBase<TParams, TResult> : BackgroundService, IJobWorker
  where TParams : class?
  where TResult : class?
{
  private readonly IOptionsMonitor<JobWorkerSettings> _workerSettings;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger _logger;
  private CancellationToken _stopToken;
  private IJobRepository<TParams, TResult>? _jobRepository;
  private IJob<TParams, TResult>? _currentJob;
  private int _executedJobs;
  private long _lastJobDurationMs;
  private DateTime? _lastJobFinishedAt;

  /// <summary>
  /// Initializes a new instance of the <see cref="JobWorkerBase{TParams, TResult}"/> class.
  /// </summary>
  /// <param name="workerSettings">An instance of an monitor for worker settings.</param>
  /// <param name="serviceProvider">An instance of the service provider.</param>
  /// <param name="logger">An instance of the logger.</param>
  protected JobWorkerBase(
    IOptionsMonitor<JobWorkerSettings> workerSettings,
    IServiceProvider serviceProvider,
    ILogger<JobWorkerBase<TParams, TResult>> logger)
  {
    _workerSettings = workerSettings;
    _serviceProvider = serviceProvider;
    _logger = logger;

    RunnerName = nameof(JobWorkerBase<TParams, TResult>);
  }

  /// <summary>
  /// Gets the name of the current runner.
  /// </summary>
  public string RunnerName { get; private set; }

  /// <summary>
  /// Exports collected metrics.
  /// </summary>
  /// <returns>An object with <c>ExecutedJobs</c> and <c>LastJobDurationMs</c>.</returns>
  public JobWorkerMetrics GetMetrics()
  {
    return new JobWorkerMetrics(_workerSettings.CurrentValue.JobType)
    {
      ExecutedJobs = _executedJobs,
      LastJobDurationMs = _lastJobDurationMs,
      JobIntervallSeconds = (int?)_workerSettings.CurrentValue.AddNextJobAfter?.TotalSeconds,
      LastJobFinishedAt = _lastJobFinishedAt,
    };
  }

  /// <summary>
  /// Should return the name of the runner. A random string is attached to it so it will be unique for each instance.
  /// </summary>
  /// <returns>A string representing the name of the runner.</returns>
  protected abstract string GetRunnerName();

  /// <summary>
  /// Returns the parameters that are used if an initial job is created.
  /// </summary>
  /// <remarks>
  /// <p>If an initial job is created the result of this function is used as the parameters of the job. The method can
  /// be overwritten to provide actual parameters, otherwise null is used.</p>
  /// <p>Only applies if an inital job is created.</p>
  /// <p>If configured the initial job will set a new job at the end using the same parameters.</p>
  /// </remarks>
  /// <returns>The parameters for the initial job.</returns>
  [TsFunction(Type = "TParams | undefined")]
  protected virtual TParams? GetInitialJobParameters()
  {
    return null;
  }

  /// <summary>
  /// Hook that is called when a job run starts.
  /// </summary>
  protected virtual void PreJobRunHook()
  {
  }

  /// <summary>
  /// Hook that is called when a job run ends.
  /// </summary>
  protected virtual void PostJobRunHook()
  {
  }

  /// <summary>
  /// Processes the job and returns the result if any.
  /// </summary>
  /// <param name="job">The job.</param>
  /// <param name="serviceScope">A service scope for injecting additional services.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The result of the job, if any.</returns>
  protected abstract Task<TResult?> ProcessJobAsync(
    IJob<TParams, TResult> job,
    [TsIgnore] IServiceScope serviceScope,
    [TsParameter(Type = "CancellationToken")] CancellationToken cancellationToken);

  /// <inheritdoc/>
  protected override async Task ExecuteAsync(
    [TsParameter(Type = "CancellationToken")] CancellationToken stoppingToken)
  {
    _stopToken = stoppingToken;

    var runnerId = Nanoid.Nanoid.Generate("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", 7);
    RunnerName = $"{GetRunnerName()}-{runnerId}";

    _logger.LogInformation(
      "Delaying runner {Runner} start for {Seconds} seconds.",
      RunnerName,
      _workerSettings.CurrentValue.InitialDelaySeconds);
    await Task.Delay(_workerSettings.CurrentValue.InitialDelaySeconds * 1000, stoppingToken);

    _logger.LogInformation("Starting runner {Runner}", RunnerName);
    var isFirstRun = true;

    while (!stoppingToken.IsCancellationRequested)
    {
      var stopwtach = new Stopwatch();
      stopwtach.Start();
      var executed = false;
      try
      {
        PreJobRunHook();
        JobWorkerSettings settings = _workerSettings.CurrentValue;
        executed = await HandleJobRunAsync(settings, stoppingToken, isFirstRun);

        PostJobRunHook();
        isFirstRun = false;
        stoppingToken.ThrowIfCancellationRequested();
        await Task.Delay(settings.JobPollingIntervalSeconds * 1000, stoppingToken);
      }
      catch (OperationCanceledException)
      {
        break;
      }
      finally
      {
        stopwtach.Stop();
        if (executed)
        {
          _lastJobDurationMs = stopwtach.ElapsedMilliseconds;
        }

        _lastJobFinishedAt = DateTime.UtcNow;
      }
    }
  }

  /// <summary>
  /// Updates the total items of the current job.
  /// </summary>
  /// <param name="items">The amount of items the job must process.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  /// <exception cref="InvalidOperationException">When no current job or repository is set.</exception>
  protected async Task SetTotalItemsAsync(int items)
  {
    CheckJobAndRepo();

    await _jobRepository.SetTotalItemsAsync(_currentJob, items, _stopToken);
  }

  /// <summary>
  /// Updates the given amount to successful items on the current job.
  /// </summary>
  /// <param name="amount">The amount to add, 1 if nothing given.</param>
  /// <returns>A taks indicating when the operation is complete.</returns>
  /// <exception cref="InvalidOperationException">When no current job or repository is set.</exception>
  protected async Task AddProgressAsync(int amount = 1)
  {
    CheckJobAndRepo();

    await _jobRepository.AddProgressAsync(_currentJob, amount, false, _stopToken);
  }

  /// <summary>
  /// Updates the given amount to successful items on the current job.
  /// </summary>
  /// <param name="amount">The amount to add, 1 if nothing given.</param>
  /// <returns>A taks indicating when the operation is complete.</returns>
  /// <exception cref="InvalidOperationException">When no current job or repository is set.</exception>
  protected async Task AddFailedProgressAsync(int amount = 1)
  {
    CheckJobAndRepo();

    await _jobRepository.AddProgressAsync(_currentJob, amount, true, _stopToken);
  }

  /// <summary>
  /// Handles a single run by checking for a new job and invoke the processing method if any.
  /// </summary>
  /// <param name="settings">An instance of the current job settings.</param>
  /// <param name="cancellationToken">A token to cancel the run.</param>
  /// <param name="firstRun">A value indicating whether this is the first run of this worker.</param>
  /// <returns>A task indicating when the run is finished.</returns>
  /// <exception cref="InvalidOperationException">
  /// When an exception is thrown during checking for a new job.
  /// </exception>
  [TsIgnore]
  private async Task<bool> HandleJobRunAsync(
    JobWorkerSettings settings,
    CancellationToken cancellationToken,
    bool firstRun = false)
  {
    var executed = false;
    using IServiceScope serviceScope = _serviceProvider.CreateScope();
    _jobRepository = serviceScope.ServiceProvider
      .GetRequiredService<IJobRepository<TParams, TResult>>();

    _currentJob = await GetJobAsync(_jobRepository, settings, cancellationToken);

    if (_currentJob != null)
    {
      try
      {
        _currentJob.Result = await ProcessJobAsync(_currentJob, serviceScope, cancellationToken);
        await _jobRepository.FinishJobAsync(_currentJob, settings.AddNextJobAfter, cancellationToken);

        _executedJobs++;
        executed = true;
      }
      catch (Exception ex)
      {
        _logger.LogWarning(
          ex,
          "Failed to process job {JobId}, job is beeing resetted. Message: {Message}",
          _currentJob.Id,
          ex.Message);

        // Since operation was cancelled (probably cause app is shutting down) no sense in forwarding our token
        await _jobRepository.ResetJobAsync(_currentJob, default);

        if (ex is OperationCanceledException)
        {
          throw;
        }
      }
    }
    else if (firstRun && settings.AddInitialJob)
    {
      await AddInitialJob(settings, cancellationToken);
    }

    _jobRepository = null;
    _currentJob = null;

    return executed;
  }

  /// <summary>
  /// Checks if there is a job available and starts the one with the oldest due date that is applicable.
  /// </summary>
  /// <param name="repo">An instance of the job repository.</param>
  /// <param name="settings">An instance of the settings for the runner.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The first applicable job or null if no job is applicable.</returns>
  [TsIgnore]
  private async Task<IJob<TParams, TResult>?> GetJobAsync(
    IJobRepository<TParams, TResult> repo,
    JobWorkerSettings settings,
    CancellationToken cancellationToken)
  {
    try
    {
      return await repo.GetAndStartFirstPendingJobAsync(settings.JobType, RunnerName, cancellationToken);
    }
    catch (Exception ex)
    {
      if (ex is OperationCanceledException)
      {
        // App is probably shutting down, just break the loop in outer method
        throw;
      }

      _logger.LogError(ex, "Error checking for next job: {Message}", ex.Message);
      throw new InvalidOperationException("Unable to check and start job.", ex);
    }
  }

  [MemberNotNull(nameof(_currentJob))]
  [MemberNotNull(nameof(_jobRepository))]
  [TsIgnore]
  private void CheckJobAndRepo()
  {
    CheckJobRepo();
    if (_currentJob == null)
    {
      throw new InvalidOperationException("Unable to set job items because no current job exists.");
    }
  }

  [MemberNotNull(nameof(_jobRepository))]
  [TsIgnore]
  private void CheckJobRepo()
  {
    if (_jobRepository == null)
    {
      throw new InvalidOperationException("Unable to set job items because no job repository is set.");
    }
  }

  [TsIgnore]
  private async Task AddInitialJob(JobWorkerSettings settings, CancellationToken cancellationToken)
  {
    CheckJobRepo();

    // Default to job should be due until 1 day from now
    var spanToCheck = TimeSpan.FromDays(1);
    if (settings.AddNextJobAfter.HasValue)
    {
      // If time between jobs is set, use this
      spanToCheck = settings.AddNextJobAfter.Value;
    }

    // calculate time until the next job should be in the database
    DateTime dueDate = DateTime.UtcNow.Add(spanToCheck);
    TParams? parameters = GetInitialJobParameters();

    // check if a job already exists that is due until the calculated date
    IJob<TParams, TResult>? existingJob = await _jobRepository.FindExistingJobAsync(
      settings.JobType,
      dueDate,
      parameters,
      true,
      cancellationToken);

    if (existingJob != null)
    {
      _logger.LogInformation(
        "Found existing job {JobId} due {DueDate}, skip adding initial job.",
        existingJob.Id,
        existingJob.DueDate);
    }
    else
    {
      // create a new job since none exist that is due until the calculated time
      await _jobRepository.AddJobAsync(settings.JobType, dueDate, parameters, cancellationToken);
    }
  }
}
