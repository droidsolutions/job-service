using System;
using System.Threading;
using System.Threading.Tasks;

using DroidSolutions.Oss.JobService;
using DroidSolutions.Oss.JobService.Worker;
using DroidSolutions.Oss.JobService.Worker.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DroidSolutions.Oss.JobService.Test.Fixture;

public class TestWorker : JobWorkerBase<TestParameter, TestResult>
{
  private Func<IJob<TestParameter, TestResult>, Task<TestResult?>>? _processFunc;

  public TestWorker(JobWorkerSettings settings, IServiceProvider serviceProvider)
    : this(
        new TestOptionsMonitor(settings),
        serviceProvider,
        new NullLoggerFactory().CreateLogger<JobWorkerBase<TestParameter, TestResult>>())
  {
  }

  public TestWorker(
    IOptionsMonitor<JobWorkerSettings> workerSettings,
    IServiceProvider serviceProvider,
    ILogger<JobWorkerBase<TestParameter, TestResult>> logger)
    : base(workerSettings, serviceProvider, logger)
  {
  }

  public bool PostHookCalled { get; private set; }

  public void SetProcessFunction(Func<IJob<TestParameter, TestResult>, Task<TestResult?>> func)
  {
    _processFunc = func;
  }

  public async Task CallSetTotalItemsAsync(int items)
  {
    await SetTotalItemsAsync(items);
  }

  public async Task CallAddProgressAsync(int progress)
  {
    await AddProgressAsync(progress);
  }

  public async Task CallAddFailedProgressAsync(int progress)
  {
    await AddFailedProgressAsync(progress);
  }

  protected override void PostJobRunHook()
  {
    base.PostJobRunHook();
    PostHookCalled = true;
  }

  protected override string GetRunnerName()
  {
    return "TestWorker";
  }

  protected override TestParameter? GetInitialJobParameters()
  {
    base.GetInitialJobParameters(); // For that extra line of coverage ¯\_(ツ)_/¯

    return new TestParameter("something");
  }

  protected override Task<TestResult?> ProcessJobAsync(
    IJob<TestParameter, TestResult> job,
    IServiceScope serviceScope,
    CancellationToken cancellationToken)
  {
    return _processFunc?.Invoke(job) ?? Task.FromResult(new TestResult
    {
      CheckedSomething = true,
      IgnoredItems = 12,
    });
  }
}
