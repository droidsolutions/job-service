using System;

using DroidSolutions.Oss.JobService.EFCore.Entity;
using DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

using FluentAssertions;

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

      job.Id.Should().Be(id);
      job.CreatedAt.Should().Be(now);
      job.DueDate.Should().Be(due);
      job.Type.Should().Be(type);
      job.State.Should().Be(state);
    }
}
