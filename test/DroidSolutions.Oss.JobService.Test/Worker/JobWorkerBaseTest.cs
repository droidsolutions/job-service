using System;
using System.Threading;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService.Test.Fixture;
using DroidSolutions.Oss.JobService.Worker.Settings;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

namespace DroidSolutions.Oss.JobService.Test.Worker;

public class JobWorkerBaseTest
{
  private readonly JobWorkerSettings _settings = new()
  {
    InitialDelaySeconds = 0,
    JobType = "Test-job",
    AddNextJobAfter = TimeSpan.FromHours(1),
    JobPollingIntervalSeconds = 100,
  };

  [Fact]
  public async Task GetJobAsync_ShouldThrowIfJobCantBeFetched()
  {
    TestOptionsMonitor optionsMon = new(_settings);

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("Test"));

    IServiceProvider provider = SetupDi(repoMock.Object);

    var logger = new NullLoggerFactory();

    var worker = new TestWorker(_settings, provider);

    Func<Task> sut = async () => { await worker.StartAsync(TestContext.Current.CancellationToken); };
    await sut.Should().ThrowAsync<InvalidOperationException>().WithMessage("Unable to check and start job.");
  }

  [Fact]
  public async Task ExecuteAsync_ShouldBreakLoop_WhenOperationCanceledExceptionIsThrown()
  {
    CancellationTokenSource cts = new();
    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Callback(cts.Cancel) // Cancel token so exception handlers matches
      .ThrowsAsync(new OperationCanceledException("Test"));

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker sut = new(_settings, provider);
    await sut.StartAsync(cts.Token);

    sut.PostHookCalled.Should().BeFalse();
  }

  [Fact]
  public async Task ExecuteAsync_ShouldBreakLoop_WhenTokenIsCanceled()
  {
    TestJob job = new() { Id = 12 };
    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker worker = new(_settings, provider);

    CancellationTokenSource cts = new();

    // Wait for the first run to complete
    cts.CancelAfter(50);
    await worker.StartAsync(cts.Token);

    repoMock.Verify(
      x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()),
      Times.Once());
    repoMock.Verify(
      x => x.FinishJobAsync(
        It.Is<TestJob>(x => x.Id == job.Id),
        _settings.AddNextJobAfter,
        It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task ExecuteAsync_ShouldResetJob_WhenTokenIsCanceled()
  {
    TestJob job = new() { Id = 12 };

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker worker = new(_settings, provider);

    worker.SetProcessFunction((x) => throw new OperationCanceledException());

    await worker.StartAsync(CancellationToken.None);
    await Task.Delay(100, TestContext.Current.CancellationToken);
    await worker.StopAsync(CancellationToken.None);

    repoMock.Verify(
      x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()),
      Times.Once());
    repoMock.Verify(
      x => x.ResetJobAsync(It.Is<TestJob>(x => x.Id == job.Id), It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task ExecuteAsync_ShouldSetRunnerName()
  {
    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker worker = new(_settings, provider);

    worker.SetProcessFunction((x) => throw new OperationCanceledException());

    await worker.StartAsync(TestContext.Current.CancellationToken);
    await worker.StopAsync(TestContext.Current.CancellationToken);

    worker.RunnerName.Should().StartWith("TestWorker-");
  }

  [Fact]
  public async Task SetTotalItemsAsync_ShouldSetProgress()
  {
    TestJob job = new() { Id = 121 };
    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker sut = new(_settings, provider);
    sut.SetProcessFunction(async (x) =>
    {
      await Task.Delay(100, TestContext.Current.CancellationToken);
      return null;
    });

    await sut.StartAsync(TestContext.Current.CancellationToken);
    await sut.CallSetTotalItemsAsync(109);
    await sut.StopAsync(TestContext.Current.CancellationToken);

    repoMock.Verify(x => x.SetTotalItemsAsync(job, 109, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact(Skip = "Find a way to setup state for this without repo being nulled after a run")]
  public async Task SetTotalItemsAsync_ShouldThrowInvalidOperationException_WhenNoCurrentJobIsSet()
  {
    var repoMock = new Mock<IJobRepository<SampleParameter, SampleResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(
        _settings.JobType,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(_settings, provider);
    Func<Task> func = async () =>
    {
      await sut.StartAsync(CancellationToken.None);
      await sut.CallSetTotalItemsAsync(1);
      await sut.StopAsync(CancellationToken.None);
    };

    await func
      .Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("Unable to set job items because no current job exists.");
  }

  [Fact]
  public async Task AddProgressAsync_ShouldThrowInvalidOperationException_WhenNoRepositoryIsSet()
  {
    IServiceProvider provider = SetupDi(null);

    TestWorker sut = new(_settings, provider);

    Func<Task> func = async () => await sut.CallAddProgressAsync(1);
    await func
      .Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("Unable to set job items because no job repository is set.");
    await sut.StopAsync(TestContext.Current.CancellationToken);
  }

  [Fact]
  public async Task AddProgressAsync_ShouldAddProgress()
  {
    TestJob job = new() { Id = 121 };

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(
        _settings.JobType,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(_settings, provider);
    sut.SetProcessFunction(async (x) =>
    {
      await Task.Delay(100, TestContext.Current.CancellationToken);
      return null;
    });

    await sut.StartAsync(TestContext.Current.CancellationToken);
    await sut.CallAddProgressAsync(14);
    await sut.StopAsync(TestContext.Current.CancellationToken);

    repoMock.Verify(x => x.AddProgressAsync(job, 14, false, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact]
  public async Task AddInitialJob_ShouldAddAnInitialJob_IfConfigured()
  {
    JobWorkerSettings settings = new()
    {
      AddInitialJob = true,
      InitialDelaySeconds = 0,
      JobPollingIntervalSeconds = 100,
      JobType = "test:job",
    };

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(
        settings.JobType,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker sut = new(settings, provider);

    DateTime from = DateTime.UtcNow.AddDays(1);
    await sut.StartAsync(TestContext.Current.CancellationToken);
    await Task.Delay(10, TestContext.Current.CancellationToken); // Give some time to add job
    await sut.StopAsync(TestContext.Current.CancellationToken);
    DateTime to = DateTime.UtcNow.AddDays(1);

    repoMock.Verify(
      x => x.AddJobAsync(
        settings.JobType,
        It.IsInRange(from, to, Moq.Range.Inclusive),
        It.Is<SampleParameter>(x => x.NotNullableString == "something"),
        It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task AddInitialJob_ShouldUseAddNextJobAfter_IfConfigured()
  {
    JobWorkerSettings settings = new()
    {
      AddInitialJob = true,
      AddNextJobAfter = TimeSpan.FromHours(3),
      InitialDelaySeconds = 0,
      JobPollingIntervalSeconds = 100,
      JobType = "test:job",
    };

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker sut = new(settings, provider);

    DateTime from = DateTime.UtcNow.AddHours(3);
    await sut.StartAsync(TestContext.Current.CancellationToken);
    await Task.Delay(10, TestContext.Current.CancellationToken); // Give some time to add job
    await sut.StopAsync(TestContext.Current.CancellationToken);
    DateTime to = DateTime.UtcNow.AddHours(3);

    repoMock.Verify(
      x => x.AddJobAsync(
        settings.JobType,
        It.IsInRange(from, to, Moq.Range.Inclusive),
        It.Is<SampleParameter>(x => x.NotNullableString == "something"),
        It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task AddInitialJob_ShouldNotAddInitialJob_IfThereIsAnyInDueRange()
  {
    JobWorkerSettings settings = new()
    {
      AddInitialJob = true,
      AddNextJobAfter = TimeSpan.FromHours(3),
      InitialDelaySeconds = 0,
      JobPollingIntervalSeconds = 100,
      JobType = "test:job",
    };
    TestJob existingJob = new()
    {
      Id = 54,
      DueDate = DateTime.UtcNow.AddMinutes(5),
    };

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(
        settings.JobType,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);
    repoMock
      .Setup(x => x.FindExistingJobAsync(
        settings.JobType,
        It.IsAny<DateTime>(),
        It.Is<SampleParameter>(x => x.NotNullableString == "something"),
        true,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(existingJob);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(settings, provider);

    DateTime from = DateTime.UtcNow.AddHours(3);
    await sut.StartAsync(CancellationToken.None);
    await Task.Delay(10, TestContext.Current.CancellationToken); // Give some time to add job
    await sut.StopAsync(CancellationToken.None);
    DateTime to = DateTime.UtcNow.AddHours(3);

    repoMock.Verify(
      x => x.AddJobAsync(
        settings.JobType,
        It.IsAny<DateTime>(),
        It.Is<SampleParameter>(x => x.NotNullableString == "something"),
        It.IsAny<CancellationToken>()),
      Times.Never());
    repoMock.Verify(
      x => x.FindExistingJobAsync(
        settings.JobType,
        It.IsInRange(from, to, Moq.Range.Inclusive),
        It.Is<SampleParameter>(x => x.NotNullableString == "something"),
        true,
        It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task AddFailedProgressAsync_ShouldAddFailedProgres()
  {
    TestJob job = new() { Id = 121 };

    Mock<IJobRepository<SampleParameter, SampleResult>> repoMock = new();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(
        _settings.JobType,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    TestWorker sut = new(_settings, provider);
    sut.SetProcessFunction(async (x) =>
    {
      await Task.Delay(100, TestContext.Current.CancellationToken);
      return null;
    });

    await sut.StartAsync(TestContext.Current.CancellationToken);
    await sut.CallAddFailedProgressAsync(7);
    await sut.StopAsync(TestContext.Current.CancellationToken);

    repoMock.Verify(x => x.AddProgressAsync(job, 7, true, It.IsAny<CancellationToken>()), Times.Once());
  }

  private static ServiceProvider SetupDi(IJobRepository<SampleParameter, SampleResult>? repo)
  {
    ServiceCollection services = new();
    services.AddScoped((x) => repo!);

    return services.BuildServiceProvider();
  }
}
