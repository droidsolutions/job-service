using System.Text.Json;

using DroidSolutions.Oss.JobService.EFCore;
using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.EFCore.Repository;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DroidSolutions.Oss.JobService.Postgres.Repository;

/// <summary>
/// A base repository to use for job management.
/// </summary>
/// <typeparam name="TContext">
/// The type of the <see cref="DbContext"/> to use. Must implement the <see cref="IJobContext{TParams, TResult}"/> interface.
/// </typeparam>
/// <typeparam name="TParams">The type of the paramters the job can have.</typeparam>
/// <typeparam name="TResult">The type of the result the job can have.</typeparam>
public class PostgresJobRepository<TContext, TParams, TResult> : JobRepositoryBase<TContext, TParams, TResult>
  where TContext : DbContext, IJobContext<TParams, TResult>
  where TParams : class?
  where TResult : class?
{
  /// <summary>
  /// Initializes a new instance of the <see cref="PostgresJobRepository{TContext, TParams, TResult}"/> class.
  /// </summary>
  /// <param name="context">An instance of a <see cref="DbContext"/> that includes the job table.</param>
  /// <param name="logger">An instance of a <see cref="ILogger"/>.</param>
  public PostgresJobRepository(TContext context, ILogger<PostgresJobRepository<TContext, TParams, TResult>> logger)
    : base(context, logger)
  {
  }

  /// <summary>
  /// Checks if a job with the given conditions already exists and returns it if so.
  /// </summary>
  /// <remarks>
  /// <p>Finds only jobs that where not started yet. Either <paramref name="dueDate"/> or <paramref name="parameters"/> must be given or an
  /// exception will be thrown.</p><br/>
  /// <p>If given the <paramref name="parameters"/> will be checked against existing parameters. See
  /// <see href="https://www.npgsql.org/efcore/mapping/json.html"/> for info of how JsonContains works.</p><br/>
  /// <p>If the <paramref name="dueDate"/> is given, only jobs that have passed the given date will be found.</p>
  /// </remarks>
  /// <param name="type">The type of the job.</param>
  /// <param name="dueDate">The date when the job should be done.</param>
  /// <param name="parameters">The parameters of the job.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The job if found or <see langword="null"/> if not.</returns>
  /// <throws Exception="InvalidOperationException">
  /// When neither <paramref name="dueDate"/> nor <paramref name="parameters"/> is given.
  /// </throws>
  public override async Task<IJob<TParams, TResult>?> FindExistingJobAsync(
    string type,
    DateTime? dueDate,
    TParams? parameters,
    CancellationToken cancellationToken = default)
  {
    if (!dueDate.HasValue && parameters == null)
    {
      throw new InvalidOperationException("Either dueDate or parameters must be given to find a job.");
    }

    IQueryable<Job<TParams, TResult>> query = Context.Jobs.Where(x => x.Type == type && x.State == JobState.Requested);

    if (dueDate.HasValue)
    {
      query = query.Where(x => x.DueDate <= dueDate);
    }

    JsonSerializerOptions? options = GetSerializerOptions();
    if (parameters != null)
    {
      var jsonString = JsonSerializer.Serialize(parameters, options);
      query = query.Where(x => x.ParametersSerialized != null && EF.Functions.JsonContains(x.ParametersSerialized, jsonString));
    }

    Job<TParams, TResult>? job = await query.FirstOrDefaultAsync(cancellationToken);
    DeserializeParameters(job);

    return job;
  }

  /// <summary>
  /// Checks the jobs table for a job that is requested and the due date passed and the given type matches. If any exist the one with the
  /// oldest due date is grabbed and updated.
  /// </summary>
  /// <remarks>
  /// <p>The <see cref="IJob{TParams, TResult}.State"/> property is set to <see cref="JobState.Started"/> and the
  /// <see cref="IJob{TParams, TResult}.UpdatedAt"/> as well as the <see cref="IJob{TParams, TResult}.Runner"/> properties are updated.</p>
  /// <br/><p>The whole operation runs in an exclusive transaction, the table is locked during the transaction so no other runner can
  /// execute this operation.</p><br/>
  /// </remarks>
  /// <param name="type">The type of the job.</param>
  /// <param name="runner">
  /// The unique name of the runner. Should contain the name, version number and a unique string.
  /// </param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The job with the oldest due date that matched the filter or null if no job matched.</returns>
  /// <exception cref="InvalidOperationException">When there are pending changes on the context.</exception>
  public override async Task<IJob<TParams, TResult>?> GetAndStartFirstPendingJobAsync(
    string type,
    string runner,
    CancellationToken cancellationToken = default)
  {
    if (Context.ChangeTracker.HasChanges())
    {
      throw new InvalidOperationException(
        "There are pending changes that would be saved, please save any pending changes before fetching next job.");
    }

    using IDbContextTransaction transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
      // lock table to prevent concurrent runners to
      var table = GetTableName(out _);

      await Context.Database.ExecuteSqlRawAsync(
        $"LOCK TABLE \"{table}\" IN ACCESS EXCLUSIVE MODE",
        cancellationToken);

      Job<TParams, TResult>? job = await Context.Jobs
        .OrderBy(x => x.DueDate)
        .FirstOrDefaultAsync(
          x => x.State == JobState.Requested && x.Type == type && x.DueDate <= DateTime.UtcNow,
          cancellationToken);

      await StartJobAsync(job, runner, cancellationToken);

      await transaction.CommitAsync(cancellationToken);

      DeserializeParameters(job);

      return job;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to fetch next job of type {JobType}: {Message}", type, ex.Message);

      await transaction.RollbackAsync(cancellationToken);

      return null;
    }
  }

  /// <summary>
  /// Adds the given amount to successful or failed items of the job to indicate progress.
  /// </summary>
  /// <remarks>
  /// The action runs in a transaction where the row is locked for update so no other runner can update the progress at the same time.
  /// </remarks>
  /// <param name="job">The job.</param>
  /// <param name="items">The amount of items to add.</param>
  /// <param name="failed">If true, amount is added to failed items instead of successful items.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task indicating when the operation is complete.</returns>
  public override async Task AddProgressAsync(
    IJob<TParams, TResult> job,
    int items = 1,
    bool failed = false,
    CancellationToken cancellationToken = default)
  {
    if (Context.ChangeTracker.HasChanges())
    {
      throw new InvalidOperationException(
        "There are pending changes that would be saved, please save any pending changes before adding progress.");
    }

    using IDbContextTransaction transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
      // lock table to prevent concurrent runners to
      var table = GetTableName(out IEntityType entityType);
      var soi = StoreObjectIdentifier.Table(table, entityType.GetSchema());
      var idColumn = entityType.GetProperty(nameof(job.Id)).GetColumnName(soi);
      var successColumn = entityType.GetProperty(nameof(job.SuccessfulItems)).GetColumnName(soi);
      var failedColumn = entityType.GetProperty(nameof(job.FailedItems)).GetColumnName(soi);

      // Select progress columns and set update lock until NPgsql implements it
      // https://github.com/npgsql/efcore.pg/issues/2049
      var data = await Context.Jobs
        .FromSqlRaw(
          $"select \"{idColumn}\", \"{successColumn}\", \"{failedColumn}\" from \"{table}\" where \"{idColumn}\" = {job.Id} FOR UPDATE")
        .Select(x => new { x.Id, x.FailedItems, x.SuccessfulItems, })
        .AsNoTracking()
        .FirstOrDefaultAsync(cancellationToken);

      if (data == null)
      {
        // No job with this id exists
        throw new InvalidOperationException(
          $"Unable to update progress because no job with id {job.Id} could be found.");
      }

      if (failed)
      {
        job.FailedItems = (data.FailedItems ?? 0) + items;
      }
      else
      {
        job.SuccessfulItems = (data.SuccessfulItems ?? 0) + items;
      }

      job.UpdatedAt = DateTime.UtcNow;

      await Context.SaveChangesAsync(cancellationToken);

      await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to update progress of job {JobId}: {Message}", job.Id, ex.Message);

      await transaction.RollbackAsync(cancellationToken);

      if (ex is InvalidOperationException)
      {
        throw;
      }
    }
  }

  private string GetTableName(out IEntityType entityType)
  {
    IEntityType? et = Context.Model.FindEntityType(typeof(Job<TParams, TResult>));
    var table = et?.GetSchemaQualifiedTableName();

    if (et is null || string.IsNullOrEmpty(table))
    {
      throw new InvalidOperationException($"Unable to table name from entity {typeof(Job<TParams, TResult>)}.");
    }

    entityType = et;

    return table;
  }
}
