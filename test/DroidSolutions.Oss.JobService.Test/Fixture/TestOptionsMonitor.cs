using System;

using DroidSolutions.Oss.JobService.Worker.Settings;

using Microsoft.Extensions.Options;

namespace DroidSolutions.Oss.JobService.Test.Fixture;

public class TestOptionsMonitor : IOptionsMonitor<JobWorkerSettings>
{
  public TestOptionsMonitor(JobWorkerSettings settings)
  {
    CurrentValue = settings;
  }

  public JobWorkerSettings CurrentValue { get; }

  public JobWorkerSettings Get(string? name)
  {
    return CurrentValue;
  }

  public IDisposable OnChange(Action<JobWorkerSettings, string> listener)
  {
    throw new NotImplementedException();
  }
}
