using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService;

/// <summary>
/// Represents a job.
/// </summary>
/// <typeparam name="TParams">The type of the job parameter.</typeparam>
/// <typeparam name="TResult">The type of the job result.</typeparam>
[TsInterface]
public interface IJob<TParams, TResult> : IJobBase
  where TParams : class?
  where TResult : class?
{
  /// <summary>
  /// Gets or sets the parameters of the job.
  /// </summary>
  TParams? Parameters { get; set; }

  /// <summary>
  /// Gets or sets the result of the job.
  /// </summary>
  TResult? Result { get; set; }
}
