namespace DroidSolutions.Oss.JobService.EFCore.Test.Fixture;

public class SampleParameter
{
  public SampleParameter(string notNullableString)
  {
    NotNullableString = notNullableString;
  }

  public string NotNullableString { get; set; }

  public string? NullableString { get; set; }

  public SampleEnumParameter Parameter { get; set; }
}
