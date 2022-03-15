using DroidSolutions.Oss.JobService.EFCore.Entity;

using Microsoft.EntityFrameworkCore;

namespace DroidSolutions.Oss.JobService.EFCore;

/// <summary>
/// A data context that integrates a generic job table.
/// </summary>
/// <typeparam name="TParams">The type of the job parameters.</typeparam>
/// <typeparam name="TResult">The type of the job result.</typeparam>
public interface IJobContext<TParams, TResult>
  where TParams : class?
  where TResult : class?
{
  /// <summary>
  /// Gets the job table.
  /// </summary>
  public DbSet<Job<TParams, TResult>> Jobs { get; }
}
