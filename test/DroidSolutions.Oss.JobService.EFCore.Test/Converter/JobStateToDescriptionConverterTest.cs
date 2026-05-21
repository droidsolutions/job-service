using System;

using DroidSolutions.Oss.JobService.EFCore.Converter;

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

    Assert.Equal("FINISHED", actual);
  }

  [Fact]
  public void ConversionToEnum_ShouldWork()
  {
    var value = "STARTED";
    var sut = new JobStateToDescriptionConverter();
    var actual = sut.ConvertFromProvider(value);

    Assert.Equal(JobState.Started, actual);
  }

  [Fact]
  public void ConversionToEnum_ShouldThrowInvalidOperationException_IfNoEnumMatches()
  {
    var value = "NONEXISTINGVALUE";
    var sut = new JobStateToDescriptionConverter();
    Func<object?> actual = () => sut.ConvertFromProvider(value);

    var ex = Assert.Throws<InvalidOperationException>(actual);
    Assert.Equal("Unable to resolve JobState enum value for string \"NONEXISTINGVALUE\".", ex.Message);
  }
}
