using System.ComponentModel;
using System.Reflection;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DroidSolutions.Oss.JobService.EFCore.Converter;

/// <summary>
/// A converter for Entity Framework Core that uses the description attribute of the jobstate as database value.
/// </summary>
public class JobStateToDescriptionConverter : ValueConverter<JobState, string>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="JobStateToDescriptionConverter"/> class.
  /// </summary>
  /// <param name="mappingHints">The mapping hints.</param>
  public JobStateToDescriptionConverter(
    ConverterMappingHints? mappingHints = null)
    : base((v) => GetDescriptionFromJobState(v), v => GetJobStateFromDescription(v), mappingHints)
  {
  }

  private static string GetDescriptionFromJobState(JobState jobState)
  {
    var description = GetEnumDescription(jobState);

    return string.IsNullOrEmpty(description) ? jobState.ToString() : description;
  }

  private static JobState GetJobStateFromDescription(string value)
  {
    foreach (JobState jobState in Enum.GetValues(typeof(JobState)))
    {
      var description = GetEnumDescription(jobState);
      if (description == value)
      {
        return jobState;
      }
    }

    throw new InvalidOperationException($"Unable to resolve JobState enum value for string \"{value}\".");
  }

  /// <summary>
  /// Returns the value of the <see cref="DescriptionAttribute"/> if it exists. Otherwise it returns the string
  /// representation of the enum.
  /// </summary>
  /// <param name="value">The enum value.</param>
  /// <returns>Either the text of the Description attribute or the string representation.</returns>
  private static string? GetEnumDescription(Enum value)
  {
    Type type = value.GetType();
    var name = Enum.GetName(type, value);
    if (name != null)
    {
      FieldInfo? field = type.GetField(name);
      if (field != null && Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
      {
        return attr.Description;
      }
    }

    return null;
  }
}