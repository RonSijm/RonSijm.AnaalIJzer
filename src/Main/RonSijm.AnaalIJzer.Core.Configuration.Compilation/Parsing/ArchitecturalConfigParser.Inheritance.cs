using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;
using RonSijm.AnaalIJzer.Core.Matchers.Symbols;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<InheritancePolicy> ParseInheritancePolicies(IEnumerable<XElement> policyElements, string ownerLayerPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<InheritancePolicy>();
		foreach (var element in policyElements)
		{
			if (!TryParseInheritancePolicyValues(element, out var typeKinds, out var requiredBaseTypes, out var requiredInterfaces, out var valueError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, valueError, element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			var policy = new InheritancePolicy(
				ownerLayerPath,
				typeKinds,
				requiredBaseTypes,
				requiredInterfaces,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0);

			policies.Add(policy);
		}

		return policies.ToImmutable();
	}

	private static bool TryParseInheritancePolicyValues(XElement element, out ImmutableHashSet<string> typeKinds, out ImmutableHashSet<string> requiredBaseTypes, out ImmutableHashSet<string> requiredInterfaces, out string error)
	{
		if (!TryParseInheritanceTypeKinds(element.Attribute("typeKinds")?.Value, out typeKinds, out error))
		{
			requiredBaseTypes = ImmutableHashSet<string>.Empty;
			requiredInterfaces = ImmutableHashSet<string>.Empty;

			return false;
		}

		if (!TryParseRequiredNames(element.Attribute("requiredBaseTypes")?.Value, "InheritancePolicy", "requiredBaseTypes", out requiredBaseTypes, out error))
		{
			requiredInterfaces = ImmutableHashSet<string>.Empty;

			return false;
		}

		if (!TryParseRequiredNames(element.Attribute("requiredInterfaces")?.Value, "InheritancePolicy", "requiredInterfaces", out requiredInterfaces, out error))
		{
			return false;
		}

		if (requiredBaseTypes.IsEmpty && requiredInterfaces.IsEmpty)
		{
			error = "InheritancePolicy requires a non-empty requiredBaseTypes or requiredInterfaces value.";

			return false;
		}

		error = string.Empty;

		return true;
	}

	private static bool TryParseInheritanceTypeKinds(string? values, out ImmutableHashSet<string> typeKinds, out string error)
	{
		if (string.IsNullOrWhiteSpace(values))
		{
			typeKinds = ImmutableHashSet<string>.Empty;
			error = "InheritancePolicy requires a non-empty typeKinds value.";

			return false;
		}

		var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		foreach (var rawValue in values!.Split(','))
		{
			var value = rawValue.Trim();
			if (value.Length == 0 || !ITypeSymbolTypeKindExtension.IsSupportedTypeKind(value))
			{
				typeKinds = ImmutableHashSet<string>.Empty;
				error = $"InheritancePolicy contains unknown type kind '{value}'. Supported values: Class, Interface, Struct, Record, RecordStruct, Enum, Delegate.";

				return false;
			}

			builder.Add(NormalizeInheritanceTypeKind(value));
		}

		typeKinds = builder.ToImmutable();
		error = string.Empty;

		return true;
	}

	private static bool TryParseRequiredNames(string? values, string elementName, string attributeName, out ImmutableHashSet<string> names, out string error)
	{
		if (string.IsNullOrWhiteSpace(values))
		{
			names = ImmutableHashSet<string>.Empty;
			error = string.Empty;

			return true;
		}

		var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		foreach (var rawValue in values!.Split(','))
		{
			var value = rawValue.Trim();
			if (value.Length == 0)
			{
				names = ImmutableHashSet<string>.Empty;
				error = elementName + " contains an empty " + attributeName + " entry.";

				return false;
			}

			builder.Add(value);
		}

		names = builder.ToImmutable();
		error = string.Empty;

		return true;
	}

	private static string NormalizeInheritanceTypeKind(string value)
	{
		var result = value.Trim().ToLowerInvariant() switch
		{
			"class" => "Class",
			"interface" => "Interface",
			"struct" => "Struct",
			"record" => "Record",
			"recordstruct" => "RecordStruct",
			"enum" => "Enum",
			"delegate" => "Delegate",
			_ => value.Trim()
		};

		return result;
	}
}
