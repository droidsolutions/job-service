namespace DroidSolutions.Oss.JobService.Worker;

/// <summary>
/// A job worker.
/// </summary>
public interface IJobWorker
{
  /// <summary>
  /// Gets the unique name of the runner.
  /// </summary>
  string RunnerName { get; }

  /// <summary>
  /// Exports collected metrics.
  /// </summary>
  /// <returns>An object with metrics of the job worker.</returns>
  JobWorkerMetrics GetMetrics();
}