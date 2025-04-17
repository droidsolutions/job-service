using System;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.Postgres.Repository;
using DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

using FluentAssertions;
using FluentAssertions.Extensions;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Repository;

[Collection("PostgresDb")]
public class PostgresJobRepositoryTest
{
  private readonly PostgresDbSetup _setup;
  private readonly PostgresJobRepository<SampleContext, SampleParameter, SampleResult> _sut;
  private readonly string _jsonString =
    "{\"notNullableString\":\"some\",\"nullableString\":\"string\",\"parameter\":0}";

  private readonly SampleParameter _testParameter = new("some")
  {
    NullableString = "string",
    Parameter = SampleEnumParameter.One,
  };

  public PostgresJobRepositoryTest(PostgresDbSetup setup)
  {
    _setup = setup;
    _sut = new PostgresJobRepository<SampleContext, SampleParameter, SampleResult>(
      setup.Context,
      new NullLogger<PostgresJobRepository<SampleContext, SampleParameter, SampleResult>>());
  }

  [Fact]
  public async Task FindExistingJob_ThrowsInvalidOperationException_WhenNoDueDateAndParametersAreGiven()
  {
    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.FindExistingJobAsync(
      "type",
      null,
      null,
      false,
      TestContext.Current.CancellationToken));
  }

  [Fact]
  public async Task FindExistingJob_ShouldFindJobByDueDate()
  {
    var type = "another-job";
    var dueDate = new DateTime(2021, 10, 26, 14, 41, 27, DateTimeKind.Utc);
    var existingJob = new Job<SampleParameter, SampleResult>
    {
      DueDate = dueDate,
      State = JobState.Requested,
      Type = type,
    };
    _setup.Context.Jobs.Add(existingJob);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? job = await _sut.FindExistingJobAsync(
      type,
      dueDate,
      null,
      false,
      TestContext.Current.CancellationToken);
    job?.Should().BeEquivalentTo(existingJob);
  }

  [Fact]
  public async Task FindExistingJob_ShouldNotFindFinishedJobByDueDate()
  {
    var type = "another-job";
    var dueDate = new DateTime(2021, 10, 26, 14, 50, 36, DateTimeKind.Utc);
    var existingJob = new Job<SampleParameter, SampleResult>
    {
      DueDate = dueDate,
      State = JobState.Finished,
      Type = type,
    };

    // ToDo maybe clear table for every test?
    _setup.Context.Jobs.RemoveRange(_setup.Context.Jobs);
    _setup.Context.Jobs.Add(existingJob);

    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? job = await _sut.FindExistingJobAsync(
      type,
      dueDate,
      null,
      false,
      TestContext.Current.CancellationToken);
    job.Should().BeNull();
  }

  [Fact]
  public async Task FindExistingJob_ShouldFindJobByParameters()
  {
    var type = "existing-job-with-params";
    var existingJob = new Job<SampleParameter, SampleResult>
    {
      ParametersSerialized = _jsonString,
      State = JobState.Requested,
      Type = type,
    };
    _setup.Context.Jobs.Add(existingJob);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? job = await _sut.FindExistingJobAsync(
      type,
      null,
      _testParameter,
      false,
      TestContext.Current.CancellationToken);
    job.Should().NotBeNull();
    job!.Parameters.Should().BeEquivalentTo(_testParameter);
  }

  [Fact]
  public async Task FindExistingJob_ShouldNotFindJobBySubsetOfParameters()
  {
    var type = "existing-job-with-params";
    var existingJob = new Job<SampleParameter, SampleResult>
    {
      ParametersSerialized = _jsonString,
      State = JobState.Requested,
      Type = type,
    };
    _setup.Context.Jobs.Add(existingJob);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    // nullable string is not set, job should not be found
    var searchParam = new SampleParameter(_testParameter.NotNullableString) { Parameter = _testParameter.Parameter };

    IJob<SampleParameter, SampleResult>? job = await _sut.FindExistingJobAsync(
      type,
      null,
      searchParam,
      false,
      TestContext.Current.CancellationToken);
    job.Should().BeNull();
  }

  [Fact]
  public async Task FindExistingJobAsync_ShouldNotFindStartedJob()
  {
    var type = "another-job";
    var dueDate = new DateTime(2022, 7, 5, 15, 8, 23, DateTimeKind.Utc);
    var existingJob = new Job<SampleParameter, SampleResult>
    {
      DueDate = dueDate,
      State = JobState.Started,
      Type = type,
    };

    // ToDo maybe clear table for every test?
    _setup.Context.Jobs.RemoveRange(_setup.Context.Jobs);
    _setup.Context.Jobs.Add(existingJob);

    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? job = await _sut.FindExistingJobAsync(
      type,
      dueDate,
      null,
      false,
      TestContext.Current.CancellationToken);
    job.Should().BeNull();
  }

  [Fact]
  public async Task FindExistingJobAsync_ShouldFindStartedJob_WhenIncludeStartedIsTrue()
  {
    var type = "another-job";
    var dueDate = new DateTime(2022, 7, 5, 15, 8, 23, DateTimeKind.Utc);
    var existingJob = new Job<SampleParameter, SampleResult>
    {
      DueDate = dueDate,
      State = JobState.Started,
      Type = type,
    };

    // ToDo maybe clear table for every test?
    _setup.Context.Jobs.RemoveRange(_setup.Context.Jobs);
    _setup.Context.Jobs.Add(existingJob);

    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? job = await _sut.FindExistingJobAsync(
      type,
      dueDate,
      null,
      true,
      TestContext.Current.CancellationToken);
    job.Should().NotBeNull();
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldThrowInvalidOperationException_WhenThereArePendingChanges()
  {
    _setup.Context.Jobs.Add(new Job<SampleParameter, SampleResult>());
    Func<Task> act = async () => await _sut.GetAndStartFirstPendingJobAsync(
      "not-starting-job",
      "usain-bolt",
      TestContext.Current.CancellationToken);
    await act.Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage(
        "There are pending changes that would be saved, please save any pending changes before fetching next job.");
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldReturnNullIfNoJobMatches()
  {
    var type = "no-matching-job";
    _setup.Context.Jobs.Add(new Job<SampleParameter, SampleResult> { Type = type, State = JobState.Finished });
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? actual = await _sut.GetAndStartFirstPendingJobAsync(
      type,
      "usain-bolt",
      TestContext.Current.CancellationToken);

    actual.Should().BeNull();
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldNotFetchJobThatIsDueInFuture()
  {
    var type = "future-job";
    _setup.Context.Jobs.Add(new Job<SampleParameter, SampleResult>
    {
      Type = type,
      State = JobState.Requested,
      DueDate = DateTime.UtcNow.AddHours(1),
    });
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? actual = await _sut.GetAndStartFirstPendingJobAsync(
      type,
      "usain-bolt",
      TestContext.Current.CancellationToken);

    actual.Should().BeNull();
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldStartOldestJob()
  {
    var type = "due-job";
    var oldJob = new Job<SampleParameter, SampleResult>
    {
      Type = type,
      State = JobState.Requested,
      DueDate = DateTime.UtcNow.AddHours(-1),
    };
    var olderJob = new Job<SampleParameter, SampleResult>
    {
      Type = type,
      State = JobState.Requested,
      DueDate = DateTime.UtcNow.AddHours(-2),
      ParametersSerialized = _jsonString,
    };
    _setup.Context.Jobs.AddRange(oldJob, olderJob);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    IJob<SampleParameter, SampleResult>? actual = await _sut.GetAndStartFirstPendingJobAsync(
      type,
      "usain-bolt",
      TestContext.Current.CancellationToken);

    actual.Should().NotBeNull();
    actual!.Id.Should().Be(olderJob.Id);
    actual!.State.Should().Be(JobState.Started);
    actual!.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
    actual!.Parameters.Should().BeEquivalentTo(_testParameter);
  }

  [Fact]
  public async Task AddProgress_ShouldThrowInvalidOperationException_WhenThereArePendingChanges()
  {
    var job = new Job<SampleParameter, SampleResult>();
    _setup.Context.Jobs.Add(job);
    Func<Task> act = async () => await _sut.AddProgressAsync(job, 1, false, TestContext.Current.CancellationToken);
    await act.Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("There are pending changes that would be saved, please save any pending changes before adding progress.");
  }

  [Fact]
  public async Task AddProgress_ShouldThrowInvalidOperationException_WhenGivenJobDoesNotExist()
  {
    var job = new Job<SampleParameter, SampleResult>() { Id = 122 };
    Func<Task> act = async () => await _sut.AddProgressAsync(job, 1, false, TestContext.Current.CancellationToken);
    await act.Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("Unable to update progress because no job with id 122 could be found.");
  }

  [Fact]
  public async Task AddProgress_ShouldDefaultToAddOneSuccessfulItem()
  {
    var job = new Job<SampleParameter, SampleResult>();
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.AddProgressAsync(job, 1, false, TestContext.Current.CancellationToken);

    job.SuccessfulItems.Should().Be(1);
    job.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
  }

  [Fact]
  public async Task AddProgress_ShouldAddCorrectAmountOfSuccessfulItems()
  {
    var job = new Job<SampleParameter, SampleResult>() { SuccessfulItems = 3 };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.AddProgressAsync(job, 4, false, TestContext.Current.CancellationToken);

    job.SuccessfulItems.Should().Be(7);
    job.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
  }

  [Fact]
  public async Task AddProgress_ShouldAddItemsToFailed_WhenFailedIsTrue()
  {
    var job = new Job<SampleParameter, SampleResult>();
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.AddProgressAsync(job, 2, true, TestContext.Current.CancellationToken);

    job.FailedItems.Should().Be(2);
    job.SuccessfulItems.Should().BeNull();
  }

  // Should be on JobRepositoryBaseTests but those run with in memory db and ExecuteDeleteAsync is not implemented
  [Fact]
  public async Task DeleteJobsAsync_ShouldDeleteOnlyJobsWithGivenType()
  {
    string deleteType = "delete-jobs";
    Job<SampleParameter, SampleResult> job1 = new() { State = JobState.Finished, Type = deleteType, };
    Job<SampleParameter, SampleResult> job2 = new() { State = JobState.Finished, Type = deleteType, };
    Job<SampleParameter, SampleResult> job3 = new() { State = JobState.Finished, Type = "non-delete-job", };
    _setup.Context.Jobs.AddRange([job1, job2, job3,]);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.DeleteJobsAsync(deleteType, null, null, TestContext.Current.CancellationToken);

    long count = await _sut.CountJobsAsync(deleteType, null, TestContext.Current.CancellationToken);
    count.Should().Be(0);

    count = await _sut.CountJobsAsync("non-delete-job", null, TestContext.Current.CancellationToken);
    count.Should().Be(1);
  }

  // Should be on JobRepositoryBaseTests but those run with in memory db and ExecuteDeleteAsync is not implemented
  [Fact]
  public async Task DeleteJobsAsync_ShouldDeleteOnlyJobsWithGivenState()
  {
    string deleteType = "delete-jobs";
    Job<SampleParameter, SampleResult> job1 = new() { State = JobState.Finished, Type = deleteType, };
    Job<SampleParameter, SampleResult> job2 = new() { State = JobState.Started, Type = deleteType, };
    Job<SampleParameter, SampleResult> job3 = new() { State = JobState.Requested, Type = deleteType, };
    _setup.Context.Jobs.AddRange([job1, job2, job3,]);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    await _sut.DeleteJobsAsync(deleteType, JobState.Finished, null, TestContext.Current.CancellationToken);

    long count = await _sut.CountJobsAsync(deleteType, null, TestContext.Current.CancellationToken);
    count.Should().Be(2);
  }

  // Should be on JobRepositoryBaseTests but those run with in memory db and ExecuteDeleteAsync is not implemented
  [Fact]
  public async Task DeleteJobsAsync_ShouldDeleteOnlyJobsOlderThenGivenDate()
  {
    string deleteType = "delete-jobs";
    DateTime refDate = new(2024, 2, 6, 15, 37, 0, DateTimeKind.Utc);
    Job<SampleParameter, SampleResult> job1 = new()
    {
      State = JobState.Finished,
      Type = deleteType,
      UpdatedAt = refDate.AddHours(-12),
    };
    Job<SampleParameter, SampleResult> job2 = new()
    {
      State = JobState.Finished,
      Type = deleteType,
      UpdatedAt = refDate.AddHours(-24),
    };
    Job<SampleParameter, SampleResult> job3 = new()
    {
      State = JobState.Requested,
      Type = deleteType,
      UpdatedAt = refDate.AddHours(-24),
    };
    _setup.Context.Jobs.AddRange([job1, job2, job3,]);
    await _setup.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

    int result = await _sut.DeleteJobsAsync(
      deleteType,
      JobState.Finished,
      refDate.AddHours(-18),
      TestContext.Current.CancellationToken);

    result.Should().Be(1);
  }
}
