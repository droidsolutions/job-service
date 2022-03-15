using System.Diagnostics;
using System.Text.Json;

using DroidSolutions.Oss.JobService.EFCore.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DroidSolutions.Oss.JobService.EFCore.Repository;

/// <summary>
/// A base repository to use for job management.
/// </summary>
/// <typeparam name="TContext">
/// The type of the <see cref="DbContext"/> to use. Must implement the <see cref="IJobContext{TParams, TResult}"/> interface.
/// </typeparam>
/// <typeparam name="TParams">The type of the paramters the job can have.</typeparam>
/// <typeparam name="TResult">The type of the result the job can have.</typeparam>
public abstract class JobRepositoryBase<TContext, TParams, TResult> : IJobRepository<TParams, TResult>
  where TContext : DbContext, IJobContext<TParams, TResult>
  where TParams : class?
  where TResult : class?
{
  /// <summary>
  /// Initializes a new instance of the <see cref="JobRepositoryBase{TContext, TParams, TResult}"/> class.
  /// </summary>
  /// <param name="context">An instance of a <see cref="DbContext"/> that includes the job table.</param>
  /// <param name="logger">An instance of a <see cref="ILogger"/>.</param>
  protected JobRepositoryBase(TContext context, ILogger logger)
  {
    Context = context;
    Logger = logger;
  }

  /// <summary>
  /// Gets an instance of the given logger.
  /// </summary>
  protected ILogger Logger { get; init; }

  /// <summary>
  /// Gets the data context.
  /// </summary>
  protected TContext Context { get; }

  /// <summary>
  /// Gets a list of internal job timers.
  /// </summary>
  protected Dictionary<long, Stopwatch> JobTimers { get; } = new();

  /// <summary>
  /// Adds a new job to the database.
  /// </summary>
  /// <param name="type">The type of the job.</param>
  /// <param name="dueDate">The date when the job should be executed. If not given current date is used. The job will not be executed until
  /// the given date has passed.</param>
  /// <param name="parameters">The parameters of the job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The added job.</returns>
  public async Task<IJob<TParams, TResult>> AddJobAsync(
    string type,
    DateTime? dueDate = null,
    TParams? parameters = null,
    CancellationToken cancellationToken = default)
  {
    var job = new Job<TParams, TResult>
    {
      CreatedAt = DateTime.UtcNow,
      DueDate = dueDate ?? DateTime.UtcNow,
      Parameters = parameters,
      State = JobState.Requested,
      Type = type,
    };

    if (parameters is not null)
    {
      JsonSerializerOptions options = GetSerializerOptions();
      job.ParametersSerialized = JsonSerializer.Serialize<TParams>(parameters, options);
    }

    Context.Jobs.Add(job);

    await Context.SaveChangesAsync(cancellationToken);

    Logger.LogInformation("Added job {JobId} with type {Type} due {Due}.", job.Id, job.Type, job.DueDate);

    return job;
  }

  /// <summary>
  /// Checks if a job with the given conditions already exists and returns it if so.
  /// </summary>
  /// <remarks>
  /// Should only find jobs that where not started yet.
  /// </remarks>
  /// <param name="type">The type of the job.</param>
  /// <param name="dueDate">The date when the job should be done.</param>
  /// <param name="parameters">The parameters of the job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The job if found or <see langword="null"/> if not.</returns>
  public abstract Task<IJob<TParams, TResult>?> FindExistingJobAsync(
    string type,
    DateTime? dueDate,
    TParams? parameters,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Should check the jobs table for a job that is requested for which the given due date is in the past and the given type matches. If any
  /// exists the one with the oldest due date should be grabbed and updated. The state should be set to <see cref="JobState.Started"/>. If
  /// no job with the given conditions exist, <see langword="null"/> may be returned.
  /// </summary>
  /// <remarks>
  /// Implementation can use the <see cref="StartJobAsync"/> method to start the job once it is found.
  /// </remarks>
  /// <param name="type">The type of the job.</param>
  /// <param name="runner">The unique name of the runner. Should contain the name, version number and a unique string.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The job with the oldest due date that matched the filter or <see langword="null"/> if no job matched.</returns>
  /// <exception cref="InvalidOperationException">When there are pending changes.</exception>
  public abstract Task<IJob<TParams, TResult>?> GetAndStartFirstPendingJobAsync(
    string type,
    string runner,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Sets the amount of total items of the job.
  /// </summary>
  /// <param name="job">The job.</param>
  /// <param name="total">The amount of items to process in the scope of this job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  public async Task SetTotalItemsAsync(IJob<TParams, TResult> job, int total, CancellationToken cancellationToken = default)
  {
    job.TotalItems = total;
    job.UpdatedAt = DateTime.UtcNow;

    await Context.SaveChangesAsync(cancellationToken);
  }

  /// <summary>
  /// Should add the given amount to successful or failed items of the job to indicate progress.
  /// </summary>
  /// <remarks>
  /// Should lock the table row to prevent others from updating the progress of this job.
  /// </remarks>
  /// <param name="job">The job.</param>
  /// <param name="items">The amount of items to add.</param>
  /// <param name="failed">If <see langword="true"/>, amount is added to failed items instead of successful items.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  public abstract Task AddProgressAsync(
    IJob<TParams, TResult> job,
    int items = 1,
    bool failed = false,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Marks the job complete, sets the result and optionally adds a new job.
  /// </summary>
  /// <remarks>
  /// <p>The given job state is set to <see cref="JobState.Finished"/> and the <see cref="IJob{TParams, TResult}.UpdatedAt"/> property is
  /// updated. If the <see cref="IJob{TParams, TResult}.Result"/> property is set the result will be serialized and set to the
  /// <see cref="Job{TParams, TResult}.ResultSerialized"/> property.</p>
  /// <p>If <paramref name="addNextJobIn"/> is given a job with the same type will be added. The due date is calculated from the current
  /// time plus the given time.</p>
  /// <p>If a timer with the id of the job exists (which is the case if the job was started via the <see cref="StartJobAsync"/> method)
  /// the timer is stopped and the <see cref="IJob{TParams, TResult}.ProcessingTimeMs"/> property is set accordingly.</p>
  /// </remarks>
  /// <param name="job">The job to complete.</param>
  /// <param name="addNextJobIn">An optional timespan when (relative to now) the next job should be run.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is completed.</returns>
  public async Task FinishJobAsync(
    IJob<TParams, TResult> job,
    TimeSpan? addNextJobIn = null,
    CancellationToken cancellationToken = default)
  {
    job.State = JobState.Finished;
    job.UpdatedAt = DateTime.UtcNow;

    if (job.Result != null && job is Job<TParams, TResult> jobEntity)
    {
      JsonSerializerOptions options = GetSerializerOptions();
      jobEntity.ResultSerialized = JsonSerializer.Serialize(job.Result, options);
    }

    if (JobTimers.TryGetValue(job.Id, out Stopwatch? watch))
    {
      watch.Stop();
      job.ProcessingTimeMs = Convert.ToUInt32(watch.ElapsedMilliseconds);
      JobTimers.Remove(job.Id);
    }

    await Context.SaveChangesAsync(cancellationToken);
    Logger.LogInformation("Finished job {JobId} with type {JobType} on runner {Runner}.", job.Id, job.Type, job.Runner);

    if (!addNextJobIn.HasValue)
    {
      return;
    }

    DateTime nextRun = DateTime.UtcNow.Add(addNextJobIn.Value);
    await AddJobAsync(job.Type, nextRun, job.Parameters, cancellationToken);
  }

  /// <summary>
  /// Resets a job back to the requested state.
  /// </summary>
  /// <remarks>
  /// Sets the <see cref="IJob{TParams, TResult}.State"/> property to <see cref="JobState.Requested"/> and the
  /// <see cref="IJob{TParams, TResult}.Runner"/> property to <see langword="null"/>.
  /// </remarks>
  /// <param name="job">The job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  public async Task ResetJobAsync(IJob<TParams, TResult> job, CancellationToken cancellationToken = default)
  {
    job.State = JobState.Requested;
    job.Runner = null;

    await Context.SaveChangesAsync(cancellationToken);

    if (JobTimers.TryGetValue(job.Id, out Stopwatch? watch))
    {
      watch.Reset();
      JobTimers.Remove(job.Id);
    }
  }

  /// <summary>
  /// Marks the given job as started and sets the runner and updated property. Also starts a timer and adds a reference to the timer to
  /// the internal timer list.
  /// </summary>
  /// <remarks>
  /// <p>Can be run in a transaction, will call Context.SaveChangesAsync so make sure no unwanted changes exist on the context!.</p>
  /// <p>The <see cref="IJob{TParams, TResult}.UpdatedAt"/> property is updated and the <see cref="IJob{TParams, TResult}.State"/> property
  /// is set to <see cref="JobState.Started"/>. Also the <see cref="IJob{TParams, TResult}.Runner"/> property is set to the value given in
  /// <paramref name="runner"/>.</p>
  /// </remarks>
  /// <param name="job">The job.</param>
  /// <param name="runner">The name of the runner.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  protected async Task StartJobAsync(Job<TParams, TResult>? job, string runner, CancellationToken cancellationToken)
  {
    if (job != null)
    {
      // start and remember stopwatch for processing time
      var stopwatch = new Stopwatch();
      stopwatch.Start();
      JobTimers.Add(job.Id, stopwatch);

      job.State = JobState.Started;
      job.UpdatedAt = DateTime.UtcNow;
      job.Runner = runner;

      await Context.SaveChangesAsync(cancellationToken);

      Logger.LogInformation("Starting job {JobId} with type {jobType} on runner {Runner}.", job.Id, job.Type, runner);
    }
  }

  /// <summary>
  /// Retrieves the instance of the <see cref="JsonSerializerOptions"/> that are used to serialize and deserialize parameters and results.
  /// </summary>
  /// <returns>An instance of <see cref="JsonSerializerOptions"/>.</returns>
  protected virtual JsonSerializerOptions GetSerializerOptions()
  {
    return new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
  }

  /// <summary>
  /// Deserializes the parameters of the given job and puts the result in the Parameters property.
  /// </summary>
  /// <param name="job">The job.</param>
  protected void DeserializeParameters(Job<TParams, TResult>? job)
  {
    if (string.IsNullOrEmpty(job?.ParametersSerialized))
    {
      return;
    }

    JsonSerializerOptions options = GetSerializerOptions();
    job.Parameters = JsonSerializer.Deserialize<TParams>(job.ParametersSerialized, options);
  }
}
