using DroidSolutions.Oss.JobService.Worker.Settings;

using Xunit;

namespace DroidSolutions.Oss.JobService.Test.Worker.Settings;

public class JobWorkerSettingsTest
{
  [Fact]
  public void Constructor_ShouldSetProperties()
  {
    var jobType = "job-settings";
    var settings = new JobWorkerSettings(jobType);

    Assert.Equal(jobType, settings.JobType);
  }
}
