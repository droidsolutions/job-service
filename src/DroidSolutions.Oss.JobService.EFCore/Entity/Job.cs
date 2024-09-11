using System.ComponentModel.DataAnnotations.Schema;

namespace DroidSolutions.Oss.JobService.EFCore.Entity;

/// <summary>
/// Represents a job with parameters and a result.
/// </summary>
/// <typeparam name="TParams">The type of the job parameter.</typeparam>
/// <typeparam name="TResult">The type of the job result.</typeparam>
public class Job<TParams, TResult> : JobBase, IJob<TParams, TResult>
  where TParams : class?
  where TResult : class?
{
  /// <summary>
  /// Gets or sets the parameters of the job.
  /// </summary>
  [Column("parameters", TypeName = "jsonb")]
  public string? ParametersSerialized { get; set; }

  /// <summary>
  /// Gets or sets the result of the job.
  /// </summary>
  [Column("result", TypeName = "jsonb")]
  public string? ResultSerialized { get; set; }

  /// <summary>
  /// Gets or sets the job parameters.
  /// </summary>
  [NotMapped]
  public TParams? Parameters { get; set; }

  /// <summary>
  /// Gets or sets the job result.
  /// </summary>
  [NotMapped]
  public TResult? Result { get; set; }
}
