using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService;

/// <summary>
/// A base repository to use for generic job management.
/// </summary>
/// <typeparam name="TParams">The type of the paramters the job can have.</typeparam>
/// <typeparam name="TResult">The type of the result the job can have.</typeparam>
[TsInterface]
[TsAddTypeImport(importTarget: "CancellationToken", importSource: "cancellationtoken")]
public interface IJobRepository<TParams, TResult>
  where TParams : class?
  where TResult : class?
{
  /// <summary>
  /// Adds a new job to the database.
  /// </summary>
  /// <param name="type">The type of the job.</param>
  /// <param name="dueDate">The date when the job should be executed. If not given current date is used.</param>
  /// <param name="parameters">The parameters of the job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The added job.</returns>
  Task<IJob<TParams, TResult>> AddJobAsync(
    string type,
    [TsParameter(Type = "Date | undefined")]
    DateTime? dueDate = null,
    TParams? parameters = null,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Adds the given amount to successful or failed items of the job to indicate progress.
  /// </summary>
  /// <param name="job">The job.</param>
  /// <param name="items">The amount of items to add. Defaults to 1.</param>
  /// <param name="failed">If <see langword="true"/>, amount is added to failed items instead of successful items.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  Task AddProgressAsync(
    IJobBase job,
    int items = 1,
    bool failed = false,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Counts the jobs in the database optionally filtered by type or state.
  /// </summary>
  /// <param name="type">The type of the job.</param>
  /// <param name="state">The state of the job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The amount of jobs in the database.</returns>
  Task<long> CountJobsAsync(
    string type,
    JobState? state = null,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks if a job with the given conditions already exists and returns it if so.
  /// </summary>
  /// <param name="type">The type of the job.</param>
  /// <param name="dueDate">The date until when the job should be done.</param>
  /// <param name="parameters">The parameters of the job.</param>
  /// <param name="includeStarted">
  /// If <see langword="true"/> job will also be found if state is <see cref="JobState.Started"/>, else only jobs that are
  /// <see cref="JobState.Requested"/> are found.
  /// </param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The job if found or null if not.</returns>
  [TsFunction(Type = "Promise<IJob<TParams, TResult> | undefined>")]
  Task<IJob<TParams, TResult>?> FindExistingJobAsync(
    string type,
    [TsParameter(Type = "Date | undefined")]
    DateTime? dueDate,
    TParams? parameters,
    bool includeStarted = false,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Marks the job complete, sets the result and optionally adds a new job.
  /// </summary>
  /// <remarks>
  /// <p>The given job state is set to finished and the UpdatedAt property is updated. If the
  /// result property is set the result will be serialized.</p>
  /// <p>If addNextJobIn is given a job with the same type will be added. The due date is calculated from the current
  /// time plus the given time.</p>
  /// </remarks>
  /// <param name="job">The job to complete.</param>
  /// <param name="addNextJobIn">An optional timespan when the next job should be run.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is completed.</returns>
  Task FinishJobAsync(
    IJob<TParams, TResult> job,
    [TsParameter(Type = "{ days?: number; hours?: number; minutes?: number; seconds?: number } | undefined")]
    TimeSpan? addNextJobIn = null,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks the jobs table for a job that is requested and the due date passed and the given type matches. If any
  /// exist the one with the oldest due date is grabbed and updated. The state is set to Started. if no job with the
  /// given conditions exist, null is returned.
  /// </summary>
  /// <remarks>
  /// Checks the jobs table for a job that is requested and the due date passed and the given type matches. If any
  /// exist the one with the oldest due date is grabbed and updated. The state is set to Started. if no job with the
  /// given conditions exist, null is returned.
  /// The whole operation runs in an exclusive transaction, the table is locked during the transaction so no other
  /// runner can execute this operation.
  /// </remarks>
  /// <param name="type">The type of the job.</param>
  /// <param name="runner">
  /// The unique name of the runner. Should contain the name, version number and a unique string.
  /// </param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The job with the oldest due date that matched the filter or null if no job matched.</returns>
  [TsFunction(Type = "Promise<IJob<TParams, TResult> | undefined>")]
  Task<IJob<TParams, TResult>?> GetAndStartFirstPendingJobAsync(
    string type,
    string runner,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Resets a job back to the requested state.
  /// </summary>
  /// <param name="job">The job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  Task ResetJobAsync(
    IJob<TParams, TResult> job,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Sets the amount of total items of the job.
  /// </summary>
  /// <param name="job">The job.</param>
  /// <param name="total">The amount of items to process in the scope of this job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  Task SetTotalItemsAsync(
    IJobBase job,
    int total,
    [TsParameter(Type = "CancellationToken", DefaultValue = "undefined")]
    CancellationToken cancellationToken = default);
}