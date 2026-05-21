using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.EFCore.Repository;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

public class InMemoryJobRepository : JobRepositoryBase<SampleContext, SampleParameter, SampleResult>
{
  public InMemoryJobRepository(SampleContext context, ILogger logger)
    : base(context, logger)
  {
  }

  public override async Task AddProgressAsync(
    IJobBase job,
    int items = 1,
    bool failed = false,
    CancellationToken cancellationToken = default)
  {
    var data = await Context.Jobs.FirstAsync(x => x.Id == job.Id, cancellationToken);
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
  }

  public override async Task<IJob<SampleParameter, SampleResult>?> FindExistingJobAsync(
    string type,
    DateTime? dueDate,
    SampleParameter? parameters,
    bool includeStarted = false,
    CancellationToken cancellationToken = default)
  {
    IQueryable<Job<SampleParameter, SampleResult>> query = Context.Jobs
      .Where(x => x.Type == type)
      .Cast<Job<SampleParameter, SampleResult>>();

    if (includeStarted)
    {
      query = query.Where(x => x.State == JobState.Started || x.State == JobState.Requested);
    }
    else
    {
      query = query.Where(x => x.State == JobState.Requested);
    }

    if (dueDate.HasValue)
    {
      query = query.Where(x => x.DueDate <= dueDate);
    }

    if (parameters != null)
    {
      // ToDo do we need this for base repo tests?
    }

    Job<SampleParameter, SampleResult>? job = await query.FirstOrDefaultAsync(cancellationToken);
    DeserializeParameters(job);

    return job;
  }

  public override async Task<IJob<SampleParameter, SampleResult>?> GetAndStartFirstPendingJobAsync(
    string type,
    string runner,
    CancellationToken cancellationToken = default)
  {
    Job<SampleParameter, SampleResult>? job = await Context.Jobs
      .OrderBy(x => x.DueDate)
      .Cast<Job<SampleParameter, SampleResult>>()
      .FirstOrDefaultAsync(
        x => x.State == JobState.Requested && x.Type == type && x.DueDate <= DateTime.UtcNow,
        cancellationToken);

    await StartJobAsync(job, runner, cancellationToken);

    return job;
  }
}
