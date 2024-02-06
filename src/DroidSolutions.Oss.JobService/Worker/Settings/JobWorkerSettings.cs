using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService.Worker.Settings;

/// <summary>
/// Settings for the job worker.
/// </summary>
[TsInterface]
public class JobWorkerSettings
{
  /// <summary>
  /// Initializes a new instance of the <see cref="JobWorkerSettings"/> class.
  /// </summary>
  public JobWorkerSettings()
  {
    JobType = string.Empty;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="JobWorkerSettings"/> class.
  /// </summary>
  /// <param name="jobType">The type of jobs the worker should process.</param>
  public JobWorkerSettings(string jobType)
  {
    JobType = jobType;
  }

  /// <summary>
  /// Gets or sets the initial delay of the worker. Defaults to 30.
  /// </summary>
  /// <remarks>
  /// When the application starts it will initialize and start each hosted service. The worker will wait the given
  /// amount of seconds before the first run is started.
  /// </remarks>
  public int InitialDelaySeconds { get; set; } = 30;

  /// <summary>
  /// Gets or sets the amount of seconds to wait before checking if a job is available.
  /// </summary>
  /// <remarks>
  /// The worker will continuously check if there is a job available. To prevent too much load on the database the
  /// interval between each check can be set. Note: The worker will not check for new jobs when a current job is
  /// beeing processed.
  /// </remarks>
  public int JobPollingIntervalSeconds { get; set; } = 10;

  /// <summary>
  /// Gets or sets a timespan that determines the due date of the job that is added after a job finished work.
  /// </summary>
  /// <remarks>
  /// When a job finishes it will trigger the next run and set the due date to the current date plus the amount of
  /// time given here. If nothing given no job will be added.
  /// </remarks>
  [TsProperty(Type = "{ days?: number; hours?: number; minutes?: number; seconds?: number }")]
  public TimeSpan? AddNextJobAfter { get; set; }

  /// <summary>
  /// Gets or sets the type of jobs the runner consumes.
  /// </summary>
  public string JobType { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether a new job should be added on the start of the worker if no job exists in
  /// the db.
  /// </summary>
  public bool AddInitialJob { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the worker should delete jobs that are older than the given timespan.
  /// </summary>
  [TsProperty(Type = "{ days?: number; hours?: number; minutes?: number; seconds?: number }")]
  public TimeSpan? DeleteJobsOlderThan { get; set; }
}
