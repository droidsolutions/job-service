using System;
using System.Threading;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;
using Xunit.Sdk;

namespace DroidSolutions.Oss.JobService.EFCore.Test.Repository;

[Collection("InMemoryDb")]
public class JobRepositoryBaseTest
{
  private readonly InMemoryDbSetup _setup;
  private readonly InMemoryJobRepository _sut;
  private readonly string _jsonString =
  "{\"notNullableString\":\"some\",\"nullableString\":\"string\",\"parameter\":0}";
  private readonly SampleParameter _testParameter = new("some")
  {
    NullableString = "string",
    Parameter = SampleEnumParameter.One,
  };

  public JobRepositoryBaseTest(InMemoryDbSetup setup)
  {
    _setup = setup;
    _sut = new InMemoryJobRepository(_setup.Context, NullLoggerFactory.Instance.CreateLogger("test"));
  }

  [Fact]
  public async Task AddJob_ShouldAddJob()
  {
    var type = "test-job";
    var dueDate = new DateTime(2021, 10, 26, 14, 3, 17, DateTimeKind.Utc);
    IJob<SampleParameter, SampleResult> job = await _sut.AddJobAsync(
      type,
      dueDate,
      null,
      TestContext.Current.CancellationToken);
    DateTime now = DateTime.UtcNow;

    Assert.Equal(type, job.Type);
    Assert.Equal(dueDate, job.DueDate);
    Assert.Null(job.Parameters);
    Assert.Equal(JobState.Requested, job.State);
    Assert.InRange(job.CreatedAt, now.AddSeconds(-10), now);
    Assert.NotEqual(0L, job.Id);
  }

  [Fact]
  public async Task AddJob_WithNoDueDate_ShouldSetNowAsDate()
  {
    var type = "test-job";
    IJob<SampleParameter, SampleResult> job = await _sut.AddJobAsync(
      type,
      null,
      null,
      TestContext.Current.CancellationToken);
    DateTime now = DateTime.UtcNow;

    Assert.InRange(job.DueDate, now.AddSeconds(-10), now);
  }

  [Fact]
  public async Task AddJob_WithParameters_ShouldSerializeThem()
  {
    var type = "test-job";
    IJob<SampleParameter, SampleResult> job = await _sut.AddJobAsync(
      type,
      null,
      _testParameter,
      TestContext.Current.CancellationToken);

    Assert.Equal(_jsonString, ((Job<SampleParameter, SampleResult>)job).ParametersSerialized);
  }

  [Fact]
  public async Task SetTotalItems_ShouldSetItemsAndUpdatedField()
  {
    var total = 12;
    var job = new Job<SampleParameter, SampleResult>
    {
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.SetTotalItemsAsync(job, total, TestContext.Current.CancellationToken);

    Assert.Equal(total, job.TotalItems);
    Assert.InRange(job.UpdatedAt!.Value, DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
  }

  [Fact]
  public async Task FinishJob_ShouldSetStateAndUpdated()
  {
    var job = new Job<SampleParameter, SampleResult>
    {
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.FinishJobAsync(job, null, TestContext.Current.CancellationToken);

    Assert.Equal(JobState.Finished, job.State);
    Assert.InRange(job.UpdatedAt!.Value, DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow);
  }

  [Fact]
  public async Task FinishJob_ShouldSetProcessingTime()
  {
    var job = new Job<SampleParameter, SampleResult>
    {
      DueDate = DateTime.UtcNow,
      Type = "processing-time",
      State = JobState.Requested,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? startedJob = await _sut.GetAndStartFirstPendingJobAsync(
      job.Type,
      "test-runner",
      CancellationToken.None);

    if (startedJob == null)
    {
      throw new XunitException("Unable to set up state for processing time test");
    }

    // Make sure at least one millisecond passed since starting the timer
    await Task.Delay(1, TestContext.Current.CancellationToken);

    await _sut.FinishJobAsync(startedJob, null, TestContext.Current.CancellationToken);

    Assert.True(startedJob.ProcessingTimeMs > 0);
  }

  [Fact]
  public async Task FinishJob_ShouldSerializeResult()
  {
    var job = new Job<SampleParameter, SampleResult>
    {
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    job.Result = new SampleResult { CheckedSomething = true, IgnoredItems = 42 };

    await _sut.FinishJobAsync(job, null, TestContext.Current.CancellationToken);

    Assert.Equal("{\"ignoredItems\":42,\"checkedSomething\":true}", job.ResultSerialized);
  }

  [Fact]
  public async Task FinishJob_ShouldAddNextJob_WhenTimespanGiven()
  {
    var type = "next-job";
    var nextJobTime = TimeSpan.FromMinutes(30);
    var job = new Job<SampleParameter, SampleResult>
    {
      Parameters = _testParameter,
      State = JobState.Started,
      Type = type,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    DateTime now = DateTime.UtcNow;
    job.Result = new SampleResult { CheckedSomething = true, IgnoredItems = 42 };

    await _sut.FinishJobAsync(job, nextJobTime, TestContext.Current.CancellationToken);

    JobBase? nextJob = await _setup.Context.Jobs
      .SingleAsync(x => x.Type == type && x.State == JobState.Requested, TestContext.Current.CancellationToken);
    var expectedDue = now.AddMinutes(30);
    Assert.InRange(nextJob.DueDate, expectedDue.AddSeconds(-10), expectedDue.AddSeconds(10));
  }

  [Fact]
  public async Task ResetJob_ShouldClearRunnerAndResetState()
  {
    Job<SampleParameter, SampleResult> job = new()
    {
      FailedItems = 2,
      Runner = "some-runner",
      State = JobState.Started,
      SuccessfulItems = 3,
      TotalItems = 12,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.ResetJobAsync(job, TestContext.Current.CancellationToken);

    Assert.Null(job.FailedItems);
    Assert.Null(job.Runner);
    Assert.Equal(JobState.Requested, job.State);
    Assert.Null(job.SuccessfulItems);
    Assert.Null(job.TotalItems);
  }

  [Fact]
  public async Task ResetJob_ShouldClearProcessingTimer()
  {
    var job = new Job<SampleParameter, SampleResult>
    {
      DueDate = DateTime.UtcNow,
      Type = "processing-time2",
      State = JobState.Requested,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? startedJob = await _sut.GetAndStartFirstPendingJobAsync(
      job.Type,
      "test-runner3",
      CancellationToken.None);

    if (startedJob == null)
    {
      throw new XunitException("Unable to set up state for processing time reset test");
    }

    await _sut.ResetJobAsync(job, TestContext.Current.CancellationToken);
    Assert.Null(job.ProcessingTimeMs);
  }

  [Fact]
  public async Task CountJob_ShouldCountAllJobs()
  {
    var job = new Job<SampleParameter, SampleResult> { State = JobState.Finished, Type = "count-all-jobs", };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    long count = await _sut.CountJobsAsync("count-all-jobs", null, TestContext.Current.CancellationToken);

    Assert.Equal(1L, count);
  }

  [Fact]
  public async Task CountJob_ShouldCountJobsByState()
  {
    var job1 = new Job<SampleParameter, SampleResult> { State = JobState.Finished, Type = "count-by-state-jobs", };
    var job2 = new Job<SampleParameter, SampleResult> { State = JobState.Started, Type = "count-by-state-jobs", };
    var job3 = new Job<SampleParameter, SampleResult> { State = JobState.Requested, Type = "count-by-state-jobs", };
    _setup.Context.Jobs.AddRange([job1, job2, job3,]);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    long count = await _sut.CountJobsAsync("count-by-state-jobs", JobState.Started, TestContext.Current.CancellationToken);

    Assert.Equal(1L, count);
  }

  [Fact]
  public async Task DeleteJobsAsync_ShouldThrow_WhenNoFilterGiven()
  {
    Func<Task> act = async () => await _sut.DeleteJobsAsync(
      string.Empty,
      null,
      null,
      TestContext.Current.CancellationToken);

    await Assert.ThrowsAsync<ArgumentException>(act);
  }
}
