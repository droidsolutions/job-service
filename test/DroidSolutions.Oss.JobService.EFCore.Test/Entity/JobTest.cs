using System;

using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

using Xunit;

namespace DroidSolutions.Oss.JobService.EFCore.Test.Entity;

public class JobTest
{
  [Fact]
  public void Constructor_ShouldSetProperties()
  {
    var id = 13;
    DateTime now = DateTime.UtcNow;
    DateTime due = DateTime.UtcNow.AddMinutes(3);
    var type = "constructor-test";
    JobState state = JobState.Requested;
    var job = new JobBase(id, now, due, state, type);

    Assert.Equal(id, job.Id);
    Assert.Equal(now, job.CreatedAt);
    Assert.Equal(due, job.DueDate);
    Assert.Equal(type, job.Type);
    Assert.Equal(state, job.State);
  }
}
