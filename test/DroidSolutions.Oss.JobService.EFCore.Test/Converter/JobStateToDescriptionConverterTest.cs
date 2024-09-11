using System;

using DroidSolutions.Oss.JobService.EFCore.Converter;

using FluentAssertions;

using Xunit;

namespace DroidSolutions.Oss.JobService.EFCore.Test.Converter;

public class JobStateToDescriptionConverterTest
{
  [Fact]
  public void ConversionFromEnum_ShouldWork()
  {
    JobState value = JobState.Finished;

    var sut = new JobStateToDescriptionConverter();
    var actual = sut.ConvertToProvider(value);

    actual.Should().Be("FINISHED");
  }

  [Fact]
  public void ConversionToEnum_ShouldWork()
  {
    var value = "STARTED";
    var sut = new JobStateToDescriptionConverter();
    var actual = sut.ConvertFromProvider(value);

    actual.Should().Be(JobState.Started);
  }

  [Fact]
  public void ConversionToEnum_ShouldThrowInvalidOperationException_IfNoEnumMatches()
  {
    var value = "NONEXISTINGVALUE";
    var sut = new JobStateToDescriptionConverter();
    Func<object?> actual = () => sut.ConvertFromProvider(value);

    actual.Should()
      .Throw<InvalidOperationException>()
      .WithMessage("Unable to resolve JobState enum value for string \"NONEXISTINGVALUE\".");
  }
}
