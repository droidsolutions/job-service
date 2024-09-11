using System;
using System.Threading;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

using FluentAssertions;
using FluentAssertions.Extensions;

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
  private readonly TestParameter _testParameter = new("some")
  {
    NullableString = "string",
    Parameter = TestEnumParameter.One,
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
    IJob<TestParameter, TestResult> job = await _sut.AddJobAsync(type, dueDate, null, default);
    DateTime now = DateTime.UtcNow;

    job.Type.Should().Be(type);
    job.DueDate.Should().Be(dueDate);
    job.Parameters.Should().BeNull();
    job.State.Should().Be(JobState.Requested);
    job.CreatedAt.Should().BeLessThan(10.Seconds()).Before(now);
    job.Id.Should().NotBe(default);
  }

  [Fact]
  public async Task AddJob_WithNoDueDate_ShouldSetNowAsDate()
  {
    var type = "test-job";
    IJob<TestParameter, TestResult> job = await _sut.AddJobAsync(type, null, null, default);
    DateTime now = DateTime.UtcNow;

    job.DueDate.Should().BeLessThan(10.Seconds()).Before(now);
  }

  [Fact]
  public async Task AddJob_WithParameters_ShouldSerializeThem()
  {
    var type = "test-job";
    IJob<TestParameter, TestResult> job = await _sut.AddJobAsync(type, null, _testParameter, default);

    ((Job<TestParameter, TestResult>)job).ParametersSerialized.Should().Be(_jsonString);
  }

  [Fact]
  public async Task SetTotalItems_ShouldSetItemsAndUpdatedField()
  {
    var total = 12;
    var job = new Job<TestParameter, TestResult>
    {
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    await _sut.SetTotalItemsAsync(job, total, default);

    job.TotalItems.Should().Be(total);
    job.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
  }

  [Fact]
  public async Task FinishJob_ShouldSetStateAndUpdated()
  {
    var job = new Job<TestParameter, TestResult>
    {
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    await _sut.FinishJobAsync(job);

    job.State.Should().Be(JobState.Finished);
    job.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
  }

  [Fact]
  public async Task FinishJob_ShouldSetProcessingTime()
  {
    var job = new Job<TestParameter, TestResult>
    {
      DueDate = DateTime.UtcNow,
      Type = "processing-time",
      State = JobState.Requested,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? startedJob = await _sut.GetAndStartFirstPendingJobAsync(
      job.Type,
      "test-runner",
      CancellationToken.None);

    if (startedJob == null)
    {
      throw new XunitException("Unable to set up state for processing time test");
    }

    // Make sure at least one millisecond passed since starting the timer
    await Task.Delay(1);

    await _sut.FinishJobAsync(startedJob);

    startedJob.ProcessingTimeMs.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task FinishJob_ShouldSerializeResult()
  {
    var job = new Job<TestParameter, TestResult>
    {
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    job.Result = new TestResult { CheckedSomething = true, IgnoredItems = 42 };

    await _sut.FinishJobAsync(job);

    job.ResultSerialized.Should().Be("{\"ignoredItems\":42,\"checkedSomething\":true}");
  }

  [Fact]
  public async Task FinishJob_ShouldAddNextJob_WhenTimespanGiven()
  {
    var type = "next-job";
    var nextJobTime = TimeSpan.FromMinutes(30);
    var job = new Job<TestParameter, TestResult>
    {
      Parameters = _testParameter,
      State = JobState.Started,
      Type = type,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    DateTime now = DateTime.UtcNow;
    job.Result = new TestResult { CheckedSomething = true, IgnoredItems = 42 };

    await _sut.FinishJobAsync(job, nextJobTime);

    JobBase? nextJob = await _setup.Context.Jobs
      .SingleAsync(x => x.Type == type && x.State == JobState.Requested);
    nextJob.DueDate.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow.AddMinutes(30));
  }

  [Fact]
  public async Task ResetJob_ShouldClearRunnerAndResetState()
  {
    var job = new Job<TestParameter, TestResult>
    {
      Runner = "some-runner",
      State = JobState.Started,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    await _sut.ResetJobAsync(job);
    job.Runner.Should().BeNull();
    job.State.Should().Be(JobState.Requested);
  }

  [Fact]
  public async Task ResetJob_ShouldClearProcessingTimer()
  {
    var job = new Job<TestParameter, TestResult>
    {
      DueDate = DateTime.UtcNow,
      Type = "processing-time2",
      State = JobState.Requested,
    };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? startedJob = await _sut.GetAndStartFirstPendingJobAsync(
      job.Type,
      "test-runner3",
      CancellationToken.None);

    if (startedJob == null)
    {
      throw new XunitException("Unable to set up state for processing time reset test");
    }

    await _sut.ResetJobAsync(job);
    job.ProcessingTimeMs.Should().BeNull();
  }

  [Fact]
  public async Task CountJob_ShouldCountAllJobs()
  {
    var job = new Job<TestParameter, TestResult> { State = JobState.Finished, Type = "count-jobs", };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    long count = await _sut.CountJobsAsync("count-jobs");

    count.Should().Be(1);
  }

  [Fact]
  public async Task CountJob_ShouldCountJobsByState()
  {
    var job1 = new Job<TestParameter, TestResult> { State = JobState.Finished, Type = "count-jobs", };
    var job2 = new Job<TestParameter, TestResult> { State = JobState.Started, Type = "count-jobs", };
    var job3 = new Job<TestParameter, TestResult> { State = JobState.Requested, Type = "count-jobs", };
    _setup.Context.Jobs.AddRange(new[] { job1, job2, job3 });
    await _setup.Context.SaveChangesAsync();

    long count = await _sut.CountJobsAsync("count-jobs", JobState.Started);

    count.Should().Be(1);
  }

  [Fact]
  public async Task DeleteJobsAsync_ShouldThrow_WhenNoFilterGiven()
  {
    Func<Task> act = async () => await _sut.DeleteJobsAsync(string.Empty, null, null, default);

    await act.Should().ThrowAsync<ArgumentException>();
  }
}
