using DroidSolutions.Oss.JobService.Internals;
using DroidSolutions.Oss.JobService.Test.Fixture;

using FluentAssertions;

using Reinforced.Typings.Ast;

using Xunit;

namespace DroidSolutions.Oss.JobService.Test.Internals;

public class EnumDescriptionToValueCodeGeneratorTest
{
  [Fact]
  public void ShouldUseEnumNameWhenNoDescriptionAttributeExists()
  {
    var rtEnum = new RtEnum();
    rtEnum.Values.Add(new RtEnumValue { EnumValueName = TestEnumParameter.One.ToString() });
    EnumDescriptionToValueCodeGenerator.ReplaceEnumValues(typeof(TestEnumParameter), rtEnum);
    rtEnum?.Values.Should().Contain(x => x.EnumValue == null && x.EnumValueName == "One");
  }

  [Fact]
  public void ShouldUseEnumDescriptionWhenDescriptionAttributeExists()
  {
    var sut = new EnumDescriptionToValueCodeGenerator();
    var rtEnum = new RtEnum();
    rtEnum.Values.Add(new RtEnumValue { EnumValueName = EnumWithDescription.One.ToString() });
    EnumDescriptionToValueCodeGenerator.ReplaceEnumValues(typeof(EnumWithDescription), rtEnum);
    rtEnum?.Values.Should().Contain(x => x.EnumValue == "\"Easy\"" && x.EnumValueName == "One");
  }
}
