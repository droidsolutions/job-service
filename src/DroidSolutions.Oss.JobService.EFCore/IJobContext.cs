using DroidSolutions.Oss.JobService.EFCore.Entity;

using Microsoft.EntityFrameworkCore;

namespace DroidSolutions.Oss.JobService.EFCore;

/// <summary>
/// A data context that integrates a generic job table.
/// </summary>
public interface IJobContext
{
  /// <summary>
  /// Gets the job table.
  /// </summary>
  public DbSet<JobBase> Jobs { get; }
}
