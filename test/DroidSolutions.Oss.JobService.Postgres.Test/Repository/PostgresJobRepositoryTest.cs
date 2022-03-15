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
  private readonly PostgresJobRepository<TestContext, TestParameter, TestResult> _sut;
  private readonly string _jsonString =
    "{\"notNullableString\":\"some\",\"nullableString\":\"string\",\"parameter\":0}";

  private readonly TestParameter _testParameter = new("some")
  {
    NullableString = "string",
    Parameter = TestEnumParameter.One,
  };

  public PostgresJobRepositoryTest(PostgresDbSetup setup)
  {
    _setup = setup;
    _sut = new PostgresJobRepository<TestContext, TestParameter, TestResult>(
      setup.Context,
      new NullLogger<PostgresJobRepository<TestContext, TestParameter, TestResult>>());
  }

  [Fact]
  public async Task FindExistingJob_ThrowsInvalidOperationException_WhenNoDueDateAndParametersAreGiven()
  {
    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.FindExistingJobAsync("type", null, null, default));
  }

  [Fact]
  public async Task FindExistingJob_ShouldFindJobByDueDate()
  {
    var type = "another-job";
    var dueDate = new DateTime(2021, 10, 26, 14, 41, 27, DateTimeKind.Utc);
    var existingJob = new Job<TestParameter, TestResult>
    {
      DueDate = dueDate,
      State = JobState.Requested,
      Type = type,
    };
    _setup.Context.Jobs.Add(existingJob);
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? job = await _sut.FindExistingJobAsync(type, dueDate, null, default);
    job?.Should().BeEquivalentTo(existingJob);
  }

  [Fact]
  public async Task FindExistingJob_ShouldNotFindFinishedJobByDueDate()
  {
    var type = "another-job";
    var dueDate = new DateTime(2021, 10, 26, 14, 50, 36, DateTimeKind.Utc);
    var existingJob = new Job<TestParameter, TestResult>
    {
      DueDate = dueDate,
      State = JobState.Finished,
      Type = type,
    };

    // ToDo maybe clear table for every test?
    _setup.Context.Jobs.RemoveRange(_setup.Context.Jobs);
    _setup.Context.Jobs.Add(existingJob);

    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? job = await _sut.FindExistingJobAsync(type, dueDate, null, default);
    job.Should().BeNull();
  }

  [Fact]
  public async Task FindExistingJob_ShouldFindJobByParameters()
  {
    var type = "existing-job-with-params";
    var existingJob = new Job<TestParameter, TestResult>
    {
      ParametersSerialized = _jsonString,
      State = JobState.Requested,
      Type = type,
    };
    _setup.Context.Jobs.Add(existingJob);
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? job = await _sut.FindExistingJobAsync(type, null, _testParameter, default);
    job.Should().NotBeNull();
    job!.Parameters.Should().BeEquivalentTo(_testParameter);
  }

  [Fact]
  public async Task FindExistingJob_ShouldNotFindJobBySubsetOfParameters()
  {
    var type = "existing-job-with-params";
    var existingJob = new Job<TestParameter, TestResult>
    {
      ParametersSerialized = _jsonString,
      State = JobState.Requested,
      Type = type,
    };
    _setup.Context.Jobs.Add(existingJob);
    await _setup.Context.SaveChangesAsync();

    // nullable string is not set, job should not be found
    var searchParam = new TestParameter(_testParameter.NotNullableString) { Parameter = _testParameter.Parameter };

    IJob<TestParameter, TestResult>? job = await _sut.FindExistingJobAsync(type, null, searchParam);
    job.Should().BeNull();
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldThrowInvalidOperationException_WhenThereArePendingChanges()
  {
    _setup.Context.Jobs.Add(new Job<TestParameter, TestResult>());
    Func<Task> act = async () => await _sut.GetAndStartFirstPendingJobAsync("not-starting-job", "usain-bolt");
    await act.Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("There are pending changes that would be saved, please save any pending changes before fetching next job.");
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldReturnNullIfNoJobMatches()
  {
    var type = "no-matching-job";
    _setup.Context.Jobs.Add(new Job<TestParameter, TestResult> { Type = type, State = JobState.Finished });
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? actual = await _sut.GetAndStartFirstPendingJobAsync(type, "usain-bolt");

    actual.Should().BeNull();
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldNotFetchJobThatIsDueInFuture()
  {
    var type = "future-job";
    _setup.Context.Jobs.Add(new Job<TestParameter, TestResult>
    {
      Type = type,
      State = JobState.Requested,
      DueDate = DateTime.UtcNow.AddHours(1),
    });
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? actual = await _sut.GetAndStartFirstPendingJobAsync(type, "usain-bolt");

    actual.Should().BeNull();
  }

  [Fact]
  public async Task GetAndStartFirstPendingJob_ShouldStartOldestJob()
  {
    var type = "due-job";
    var oldJob = new Job<TestParameter, TestResult>
    {
      Type = type,
      State = JobState.Requested,
      DueDate = DateTime.UtcNow.AddHours(-1),
    };
    var olderJob = new Job<TestParameter, TestResult>
    {
      Type = type,
      State = JobState.Requested,
      DueDate = DateTime.UtcNow.AddHours(-2),
      ParametersSerialized = _jsonString,
    };
    _setup.Context.Jobs.AddRange(oldJob, olderJob);
    await _setup.Context.SaveChangesAsync();

    IJob<TestParameter, TestResult>? actual = await _sut.GetAndStartFirstPendingJobAsync(type, "usain-bolt");

    actual.Should().NotBeNull();
    actual!.Id.Should().Be(olderJob.Id);
    actual!.State.Should().Be(JobState.Started);
    actual!.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
    actual!.Parameters.Should().BeEquivalentTo(_testParameter);
  }

  [Fact]
  public async Task AddProgress_ShouldThrowInvalidOperationException_WhenThereArePendingChanges()
  {
    var job = new Job<TestParameter, TestResult>();
    _setup.Context.Jobs.Add(job);
    Func<Task> act = async () => await _sut.AddProgressAsync(job);
    await act.Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("There are pending changes that would be saved, please save any pending changes before adding progress.");
  }

  [Fact]
  public async Task AddProgress_ShouldThrowInvalidOperationException_WhenGivenJobDoesNotExist()
  {
    var job = new Job<TestParameter, TestResult>() { Id = 122 };
    Func<Task> act = async () => await _sut.AddProgressAsync(job);
    await act.Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("Unable to update progress because no job with id 122 could be found.");
  }

  [Fact]
  public async Task AddProgress_ShouldDefaultToAddOneSuccessfulItem()
  {
    var job = new Job<TestParameter, TestResult>();
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    await _sut.AddProgressAsync(job);

    job.SuccessfulItems.Should().Be(1);
    job.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
  }

  [Fact]
  public async Task AddProgress_ShouldAddCorrectAmountOfSuccessfulItems()
  {
    var job = new Job<TestParameter, TestResult>() { SuccessfulItems = 3 };
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    await _sut.AddProgressAsync(job, 4);

    job.SuccessfulItems.Should().Be(7);
    job.UpdatedAt.Should().BeLessThan(10.Seconds()).Before(DateTime.UtcNow);
  }

  [Fact]
  public async Task AddProgress_ShouldAddItemsToFailed_WhenFailedIsTrue()
  {
    var job = new Job<TestParameter, TestResult>();
    _setup.Context.Jobs.Add(job);
    await _setup.Context.SaveChangesAsync();

    await _sut.AddProgressAsync(job, 2, true);

    job.FailedItems.Should().Be(2);
    job.SuccessfulItems.Should().BeNull();
  }
}
