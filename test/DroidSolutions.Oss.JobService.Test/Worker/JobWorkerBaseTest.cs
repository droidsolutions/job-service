using System;
using System.Threading;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService;
using DroidSolutions.Oss.JobService.Worker.Settings;
using DroidSolutions.Oss.JobService.Test.Fixture;

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
    var optionsMon = new TestOptionsMonitor(_settings);

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("Test"));

    IServiceProvider provider = SetupDi(repoMock.Object);

    var logger = new NullLoggerFactory();

    var worker = new TestWorker(_settings, provider);

    Func<Task> sut = async () => { await worker.StartAsync(CancellationToken.None); };
    await sut.Should().ThrowAsync<InvalidOperationException>().WithMessage("Unable to check and start job.");
  }

  [Fact]
  public async Task ExecuteAsync_ShouldBreakLoop_WhenOperationCanceledExceptionIsThrown()
  {
    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OperationCanceledException("Test"));

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(_settings, provider);
    await sut.StartAsync(CancellationToken.None);

    sut.PostHookCalled.Should().BeFalse();
  }

  [Fact]
  public async Task ExecuteAsync_ShouldBreakLoop_WhenTokenIsCanceled()
  {
    var job = new TestJob() { Id = 12 };
    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var worker = new TestWorker(_settings, provider);

    var cts = new CancellationTokenSource();

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
    var job = new TestJob() { Id = 12 };

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var worker = new TestWorker(_settings, provider);

    worker.SetProcessFunction((x) => throw new OperationCanceledException());

    await worker.StartAsync(CancellationToken.None);
    await Task.Delay(100);
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
    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();

    IServiceProvider provider = SetupDi(repoMock.Object);

    var worker = new TestWorker(_settings, provider);

    worker.SetProcessFunction((x) => throw new OperationCanceledException());

    await worker.StartAsync(CancellationToken.None);
    await worker.StopAsync(CancellationToken.None);

    worker.RunnerName.Should().StartWith("TestWorker-");
  }

  [Fact]
  public async Task SetTotalItemsAsync_ShouldSetProgress()
  {
    var job = new TestJob() { Id = 121 };
    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(_settings, provider);
    sut.SetProcessFunction(async (x) =>
    {
      await Task.Delay(100);
      return null;
    });

    await sut.StartAsync(CancellationToken.None);
    await sut.CallSetTotalItemsAsync(109);
    await sut.StopAsync(CancellationToken.None);

    repoMock.Verify(x => x.SetTotalItemsAsync(job, 109, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact(Skip = "Find a way to setup state for this without repo being nulled after a run")]
  public async Task SetTotalItemsAsync_ShouldThrowInvalidOperationException_WhenNoCurrentJobIsSet()
  {
    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

    var sut = new TestWorker(_settings, provider);

    Func<Task> func = async () => await sut.CallAddProgressAsync(1);
    await func
      .Should()
      .ThrowAsync<InvalidOperationException>()
      .WithMessage("Unable to set job items because no job repository is set.");
    await sut.StopAsync(CancellationToken.None);
  }

  [Fact]
  public async Task AddProgressAsync_ShouldAddProgress()
  {
    var job = new TestJob() { Id = 121 };

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(_settings, provider);
    sut.SetProcessFunction(async (x) =>
    {
      await Task.Delay(100);
      return null;
    });

    await sut.StartAsync(CancellationToken.None);
    await sut.CallAddProgressAsync(14);
    await sut.StopAsync(CancellationToken.None);

    repoMock.Verify(x => x.AddProgressAsync(job, 14, false, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact]
  public async Task AddInitialJob_ShouldAddAnInitialJob_IfConfigured()
  {
    var settings = new JobWorkerSettings
    {
      AddInitialJob = true,
      InitialDelaySeconds = 0,
      JobPollingIntervalSeconds = 100,
      JobType = "test:job",
    };

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(settings, provider);

    DateTime from = DateTime.UtcNow.AddDays(1);
    await sut.StartAsync(CancellationToken.None);
    await Task.Delay(10); // Give some time to add job
    await sut.StopAsync(CancellationToken.None);
    DateTime to = DateTime.UtcNow.AddDays(1);

    repoMock.Verify(
      x => x.AddJobAsync(
        settings.JobType,
        It.IsInRange(from, to, Moq.Range.Inclusive),
        It.Is<TestParameter>(x => x.NotNullableString == "something"),
        It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task AddInitialJob_ShouldUseAddNextJobAfter_IfConfigured()
  {
    var settings = new JobWorkerSettings
    {
      AddInitialJob = true,
      AddNextJobAfter = TimeSpan.FromHours(3),
      InitialDelaySeconds = 0,
      JobPollingIntervalSeconds = 100,
      JobType = "test:job",
    };

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(settings, provider);

    DateTime from = DateTime.UtcNow.AddHours(3);
    await sut.StartAsync(CancellationToken.None);
    await Task.Delay(10); // Give some time to add job
    await sut.StopAsync(CancellationToken.None);
    DateTime to = DateTime.UtcNow.AddHours(3);

    repoMock.Verify(
      x => x.AddJobAsync(
        settings.JobType,
        It.IsInRange(from, to, Moq.Range.Inclusive),
        It.Is<TestParameter>(x => x.NotNullableString == "something"),
        It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task AddInitialJob_ShouldNotAddInitialJob_IfThereIsAnyInDueRange()
  {
    var settings = new JobWorkerSettings
    {
      AddInitialJob = true,
      AddNextJobAfter = TimeSpan.FromHours(3),
      InitialDelaySeconds = 0,
      JobPollingIntervalSeconds = 100,
      JobType = "test:job",
    };
    var existingJob = new TestJob
    {
      Id = 54,
      DueDate = DateTime.UtcNow.AddMinutes(5),
    };

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((TestJob?)null);
    repoMock
      .Setup(x => x.FindExistingJobAsync(
        settings.JobType,
        It.IsAny<DateTime>(),
        It.Is<TestParameter>(x => x.NotNullableString == "something"),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(existingJob);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(settings, provider);

    DateTime from = DateTime.UtcNow.AddHours(3);
    await sut.StartAsync(CancellationToken.None);
    await Task.Delay(10); // Give some time to add job
    await sut.StopAsync(CancellationToken.None);
    DateTime to = DateTime.UtcNow.AddHours(3);

    repoMock.Verify(
      x => x.AddJobAsync(settings.JobType, It.IsAny<DateTime>(), It.Is<TestParameter>(x => x.NotNullableString == "something"), It.IsAny<CancellationToken>()),
      Times.Never());
    repoMock.Verify(
      x => x.FindExistingJobAsync(settings.JobType, It.IsInRange(from, to, Moq.Range.Inclusive), It.Is<TestParameter>(x => x.NotNullableString == "something"), It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task AddFailedProgressAsync_ShouldAddFailedProgres()
  {
    var job = new TestJob() { Id = 121 };

    var repoMock = new Mock<IJobRepository<TestParameter, TestResult>>();
    repoMock
      .Setup(x => x.GetAndStartFirstPendingJobAsync(_settings.JobType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(job);

    IServiceProvider provider = SetupDi(repoMock.Object);

    var sut = new TestWorker(_settings, provider);
    sut.SetProcessFunction(async (x) =>
    {
      await Task.Delay(100);
      return null;
    });

    await sut.StartAsync(CancellationToken.None);
    await sut.CallAddFailedProgressAsync(7);
    await sut.StopAsync(CancellationToken.None);

    repoMock.Verify(x => x.AddProgressAsync(job, 7, true, It.IsAny<CancellationToken>()), Times.Once());
  }

  private static IServiceProvider SetupDi(IJobRepository<TestParameter, TestResult>? repo)
  {
    var services = new ServiceCollection();
    services.AddScoped((x) => repo!);

    return services.BuildServiceProvider();
  }
}
