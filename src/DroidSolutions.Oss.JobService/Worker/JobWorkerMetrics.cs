using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService.Worker;

/// <summary>
/// Available metrics of a job worker.
/// </summary>
[TsInterface]
public class JobWorkerMetrics
{
  /// <summary>
  /// Initializes a new instance of the <see cref="JobWorkerMetrics"/> class.
  /// </summary>
  /// <param name="jobType">The type of jobs the worker is processing.</param>
  public JobWorkerMetrics(string jobType)
  {
    JobType = jobType;
  }

  /// <summary>
  /// Gets the type of jobs the runner executes.
  /// </summary>
  public string JobType { get; init; }

  /// <summary>
  /// Gets the count of jobs the runner executed.
  /// </summary>
  public int ExecutedJobs { get; init; }

  /// <summary>
  /// Gets the duration time in milleseconds of the last job execution.
  /// </summary>
  public long LastJobDurationMs { get; init; }

  /// <summary>
  /// Gets or sets the date when the last job finished running.
  /// </summary>
  [TsProperty(Type = "Date")]
  public DateTime? LastJobFinishedAt { get; set; }

  /// <summary>
  /// Gets or sets the time in seconds that are between 2 job executions.
  /// </summary>
  public int? JobIntervallSeconds { get; set; }
}
