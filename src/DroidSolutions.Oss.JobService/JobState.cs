using System.ComponentModel;

using DroidSolutions.Oss.JobService.Internals;

using Reinforced.Typings.Attributes;

[assembly: TsGlobal(
  AutoAsync = true,
  AutoOptionalProperties = true,
  CamelCaseForMethods = true,
  CamelCaseForProperties = true,
  DiscardNamespacesWhenUsingModules = true,
  GenerateDocumentation = true,
  RootNamespace = "DroidSolutions.Oss.JobService",
  UseModules = true,
  TabSymbol = "  ")]

namespace DroidSolutions.Oss.JobService;

/// <summary>
/// Defines the states a job can be in.
/// </summary>
[TsEnum(CodeGeneratorType = typeof(EnumDescriptionToValueCodeGenerator))]
public enum JobState
{
  /// <summary>
  /// The job was created but no runner took it.
  /// </summary>
  [Description("REQUESTED")]
  Requested,

  /// <summary>
  /// The job was picked up by a runner and is currently being processed.
  /// </summary>
  [Description("STARTED")]
  Started,

  /// <summary>
  /// The job is done.
  /// </summary>
  [Description("FINISHED")]
  Finished,
}
