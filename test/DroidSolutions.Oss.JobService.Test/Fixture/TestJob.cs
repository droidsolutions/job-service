using System;

using DroidSolutions.Oss.JobService;

namespace DroidSolutions.Oss.JobService.Test.Fixture;

public class TestJob : IJob<SampleParameter, SampleResult>
{
  public long Id { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public DateTime DueDate { get; set; }
  public JobState State { get; set; }
  public string Type { get; set; } = "test";
  public int? TotalItems { get; set; }
  public int? SuccessfulItems { get; set; }
  public int? FailedItems { get; set; }
  public SampleParameter? Parameters { get; set; }
  public SampleResult? Result { get; set; }
  public string? Runner { get; set; }
  public uint? ProcessingTimeMs { get; set; }
}
