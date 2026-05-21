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

public class TestWorker(
  IOptionsMonitor<JobWorkerSettings> workerSettings,
  IServiceProvider serviceProvider,
  ILogger<JobWorkerBase<SampleParameter, SampleResult>> logger)
  : JobWorkerBase<SampleParameter, SampleResult>(workerSettings, serviceProvider, logger)
{
  private Func<IJob<SampleParameter, SampleResult>, CancellationToken, Task<SampleResult?>>? _processFunc;

  public TestWorker(JobWorkerSettings settings, IServiceProvider serviceProvider)
    : this(
        new TestOptionsMonitor(settings),
        serviceProvider,
        new NullLoggerFactory().CreateLogger<JobWorkerBase<SampleParameter, SampleResult>>())
  {
  }

  public bool PostHookCalled { get; private set; }

  public void SetProcessFunction(Func<IJob<SampleParameter, SampleResult>, CancellationToken, Task<SampleResult?>> func)
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

  protected override ValueTask PostJobRunHookAsync(CancellationToken cancellationToken)
  {
    PostHookCalled = true;
    return base.PostJobRunHookAsync(cancellationToken);
  }

  protected override string GetRunnerName()
  {
    return "TestWorker";
  }

  protected override SampleParameter? GetInitialJobParameters()
  {
    base.GetInitialJobParameters(); // For that extra line of coverage ¯\_(ツ)_/¯

    return new SampleParameter("something");
  }

  protected override async Task<SampleResult?> ProcessJobAsync(
    IJob<SampleParameter, SampleResult> job,
    IServiceScope serviceScope,
    CancellationToken cancellationToken)
  {
    return _processFunc == null ? null : await _processFunc.Invoke(job, cancellationToken);
  }
}
