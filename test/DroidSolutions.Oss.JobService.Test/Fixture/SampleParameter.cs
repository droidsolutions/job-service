namespace DroidSolutions.Oss.JobService.Test.Fixture;

public class SampleParameter
{
  public SampleParameter(string notNullableString)
  {
    NotNullableString = notNullableString;
  }

  public string NotNullableString { get; set; }

  public string? NullableString { get; set; }

  public TestEnumParameter Parameter { get; set; }
}
