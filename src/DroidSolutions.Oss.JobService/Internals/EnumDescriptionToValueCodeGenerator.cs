using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

using Reinforced.Typings;
using Reinforced.Typings.Ast;
using Reinforced.Typings.Generators;

[assembly: InternalsVisibleTo("DroidSolutions.Oss.JobService.Test")]

namespace DroidSolutions.Oss.JobService.Internals;

/// <summary>
/// A code generator that uses the description attribute of C# enums to generate corresponding TypeScript enums.
/// </summary>
internal class EnumDescriptionToValueCodeGenerator : EnumGenerator
{
  /// <summary>
  /// Loops through the values of the node and checks if the corresponding enum value has a description attribute set.
  /// If found the value of the description attribute will be set to EnumValue.
  /// </summary>
  /// <param name="enumType">The type of the enum.</param>
  /// <param name="node">The enum node.</param>
  public static void ReplaceEnumValues(Type enumType, RtEnum node)
  {
    foreach (RtEnumValue enumValue in node.Values)
    {
      var test = Enum.Parse(enumType, enumValue.EnumValueName);
      var name = GetDescription((Enum)test);
      if (name != null)
      {
        enumValue.EnumValue = $"\"{name}\"";
      }
    }
  }

  /// <inheritdoc/>
  public override RtEnum? GenerateNode(Type element, RtEnum result, TypeResolver resolver)
  {
    RtEnum node = base.GenerateNode(element, result, resolver);

    if (node != null)
    {
      ReplaceEnumValues(element, node);
    }

    return node;
  }

  private static string? GetDescription(Enum value)
  {
    Type type = value.GetType();
    var name = Enum.GetName(type, value);
    if (name != null)
    {
      FieldInfo? field = type.GetField(name);
      if (field != null)
      {
        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
        {
          return attr.Description;
        }
      }
    }

    return null;
  }
}
