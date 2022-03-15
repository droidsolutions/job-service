namespace DroidSolutions.Oss.JobService.Test.Fixture;

public class TestParameter
{
  public TestParameter(string notNullableString)
  {
    NotNullableString = notNullableString;
  }

  public string NotNullableString { get; set; }

  public string? NullableString { get; set; }

  public TestEnumParameter Parameter { get; set; }
}
