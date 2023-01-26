using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService;

/// <summary>
/// Represents the base job without arguments or a result.
/// </summary>
[TsInterface]
public interface IJobBase
{
  /// <summary>
  /// Gets or sets a unique identifier of the entity.
  /// </summary>
  long Id { get; set; }

  /// <summary>
  /// Gets or sets the time when the job was added to the database.
  /// </summary>
  [TsProperty(Type = "Date")]
  DateTime CreatedAt { get; set; }

  /// <summary>
  /// Gets or sets the time when the job was last edited.
  /// </summary>
  [TsProperty(Type = "Date")]
  DateTime? UpdatedAt { get; set; }

  /// <summary>
  /// Gets or sets the earliest time when the job should be done.
  /// </summary>
  [TsProperty(Type = "Date")]
  DateTime DueDate { get; set; }

  /// <summary>
  /// Gets or sets the current state of the job.
  /// </summary>
  JobState State { get; set; }

  /// <summary>
  /// Gets or sets the type of the job.
  /// </summary>
  string Type { get; set; }

  /// <summary>
  /// Gets or sets thename of the runner that executed the job.
  /// </summary>
  string? Runner { get; set; }

  /// <summary>
  /// Gets or sets the time the job took to finish.
  /// </summary>
  uint? ProcessingTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the amount of items the job must process.
  /// </summary>
  int? TotalItems { get; set; }

  /// <summary>
  /// Gets or sets the amount of items that were successfully processed.
  /// </summary>
  int? SuccessfulItems { get; set; }

  /// <summary>
  /// Gets or sets the amount of items that failed processing.
  /// </summary>
  int? FailedItems { get; set; }
}
