using System.ComponentModel;

namespace DroidSolutions.Oss.JobService.Test.Fixture;

public enum EnumWithDescription
{
  [Description("Easy")]
  One,

  [Description("Hard")]
  Two,
}
