using System.ComponentModel.DataAnnotations;

using Reinforced.Typings.Attributes;

namespace DroidSolutions.Oss.JobService.EFCore.Entity;

/// <summary>
/// Represents a job without parameters or result.
/// </summary>
public class JobBase : IJobBase
{
  /// <summary>
  /// Initializes a new instance of the <see cref="JobBase"/> class.
  /// </summary>
  public JobBase()
  {
    Type = string.Empty;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="JobBase"/> class.
  /// </summary>
  /// <param name="id">The id of the entity.</param>
  /// <param name="createdAt">The date when the entry was created.</param>
  /// <param name="dueDate">The date when the job should be run.</param>
  /// <param name="state">The state of the job.</param>
  /// <param name="type">The type of the job.</param>
  public JobBase(long id, DateTime createdAt, DateTime dueDate, JobState state, string type)
  {
    Id = id;
    CreatedAt = createdAt;
    DueDate = dueDate;
    State = state;
    Type = type;
  }

  /// <summary>
  /// Gets or sets a unique identifier of the entity.
  /// </summary>
  [Key]
  public long Id { get; set; }

  /// <summary>
  /// Gets or sets the time when the job was added to the database.
  /// </summary>
  [Required]
  [TsProperty(Type = "Date")]
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Gets or sets the time when the job was last edited.
  /// </summary>
  [TsProperty(Type = "Date")]
  public DateTime? UpdatedAt { get; set; }

  /// <summary>
  /// Gets or sets the earliest time when the job should be done.
  /// </summary>
  [Required]
  [TsProperty(Type = "Date")]
  public DateTime DueDate { get; set; }

  /// <summary>
  /// Gets or sets the current state of the job.
  /// </summary>
  [Required]
  public JobState State { get; set; }

  /// <summary>
  /// Gets or sets the type of the job.
  /// </summary>
  [Required]
  public string Type { get; set; }

  /// <summary>
  /// Gets or sets the amount of items the job must process.
  /// </summary>
  public int? TotalItems { get; set; }

  /// <summary>
  /// Gets or sets the amount of items that were successfully processed.
  /// </summary>
  public int? SuccessfulItems { get; set; }

  /// <summary>
  /// Gets or sets the amount of items that failed processing.
  /// </summary>
  public int? FailedItems { get; set; }

  /// <summary>
  /// Gets or sets the runner that executed the job.
  /// </summary>
  public string? Runner { get; set; }

  /// <summary>
  /// Gets or sets the time the job took to finish.
  /// </summary>
  public uint? ProcessingTimeMs { get; set; }
}
