using System;

using DroidSolutions.Oss.JobService;

namespace DroidSolutions.Oss.JobService.Test.Fixture;

public class TestJob : IJob<TestParameter, TestResult>
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
  public TestParameter? Parameters { get; set; }
  public TestResult? Result { get; set; }
  public string? Runner { get; set; }
  public uint? ProcessingTimeMs { get; set; }
}
