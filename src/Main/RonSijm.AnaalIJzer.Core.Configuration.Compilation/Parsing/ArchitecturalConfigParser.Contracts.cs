using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Contracts;
using RonSijm.AnaalIJzer.Symbols;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<ContractPolicy> ParseContractPolicies(IEnumerable<XElement> policyElements, string ownerLayerPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<ContractPolicy>();
		foreach (var element in policyElements)
		{
			if (!TryParseContractPolicyValues(element, out var allowedTypeKinds, out var allowedMemberKinds, out var allowedPropertyAccessors, out var restrictPropertyAccessors, out var valueError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, valueError, element, xmlPath);
				continue;
			}

			if (!TryReadBooleanAttribute(element, "allowMethodBodies", out var allowMethodBodies))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ContractPolicy contains an invalid allowMethodBodies value. Use true, false, 1, or 0.", element, xmlPath);
				continue;
			}

			if (!TryReadBooleanAttribute(element, "allowStaticMembers", out var allowStaticMembers))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ContractPolicy contains an invalid allowStaticMembers value. Use true, false, 1, or 0.", element, xmlPath);
				continue;
			}

			if (!TryReadBooleanAttribute(element, "allowNestedTypes", out var allowNestedTypes))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ContractPolicy contains an invalid allowNestedTypes value. Use true, false, 1, or 0.", element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			var policy = new ContractPolicy(
				ownerLayerPath,
				allowedTypeKinds,
				allowedMemberKinds,
				allowedPropertyAccessors,
				restrictPropertyAccessors,
				allowMethodBodies,
				allowStaticMembers,
				allowNestedTypes,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0);

			policies.Add(policy);
		}

		return policies.ToImmutable();
	}

	private static bool TryParseContractPolicyValues(XElement element, out ImmutableHashSet<string> allowedTypeKinds, out ImmutableHashSet<ContractMemberKind> allowedMemberKinds, out ImmutableHashSet<ContractPropertyAccessor> allowedPropertyAccessors, out bool restrictPropertyAccessors, out string error)
	{
		if (!TryParseContractTypeKinds(element.Attribute("allowedTypeKinds")?.Value, out allowedTypeKinds, out error))
		{
			allowedMemberKinds = ImmutableHashSet<ContractMemberKind>.Empty;
			allowedPropertyAccessors = ImmutableHashSet<ContractPropertyAccessor>.Empty;
			restrictPropertyAccessors = false;
			return false;
		}

		if (!TryParseContractMemberKinds(element.Attribute("allowedMemberKinds")?.Value, out allowedMemberKinds, out error))
		{
			allowedPropertyAccessors = ImmutableHashSet<ContractPropertyAccessor>.Empty;
			restrictPropertyAccessors = false;
			return false;
		}

		if (!TryParseContractPropertyAccessors(element.Attribute("allowedPropertyAccessors")?.Value, out allowedPropertyAccessors, out restrictPropertyAccessors, out error))
		{
			return false;
		}

		error = string.Empty;
		return true;
	}

	private static bool TryParseContractTypeKinds(string? values, out ImmutableHashSet<string> allowedTypeKinds, out string error)
	{
		if (string.IsNullOrWhiteSpace(values))
		{
			allowedTypeKinds = ImmutableHashSet<string>.Empty;
			error = "ContractPolicy requires a non-empty allowedTypeKinds value.";
			return false;
		}

		var rawValues = values!.Split(',');
		var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		foreach (var rawValue in rawValues)
		{
			var value = rawValue.Trim();
			if (value.Length == 0 || !ITypeSymbolTypeKindExtension.IsSupportedTypeKind(value))
			{
				allowedTypeKinds = ImmutableHashSet<string>.Empty;
				error = $"ContractPolicy contains unknown type kind '{value}'. Supported values: Class, Interface, Struct, Record, RecordStruct, Enum, Delegate.";
				return false;
			}

			builder.Add(NormalizeContractTypeKind(value));
		}

		allowedTypeKinds = builder.ToImmutable();
		error = string.Empty;
		return true;
	}

	private static bool TryParseContractMemberKinds(string? values, out ImmutableHashSet<ContractMemberKind> allowedMemberKinds, out string error)
	{
		if (string.IsNullOrWhiteSpace(values))
		{
			allowedMemberKinds = ImmutableHashSet<ContractMemberKind>.Empty;
			error = "ContractPolicy requires a non-empty allowedMemberKinds value.";
			return false;
		}

		var rawValues = values!.Split(',');
		var builder = ImmutableHashSet.CreateBuilder<ContractMemberKind>();
		foreach (var rawValue in rawValues)
		{
			var trimmedValue = rawValue.Trim();
			if (!ContractMemberKindParser.TryParse(rawValue, out var memberKind))
			{
				allowedMemberKinds = ImmutableHashSet<ContractMemberKind>.Empty;
				error = $"ContractPolicy contains unknown member kind '{trimmedValue}'. Supported values: {string.Join(", ", ContractMemberKindParser.CanonicalOrder)}.";
				return false;
			}

			builder.Add(memberKind);
		}

		allowedMemberKinds = builder.ToImmutable();
		error = string.Empty;
		return true;
	}

	private static bool TryParseContractPropertyAccessors(string? values, out ImmutableHashSet<ContractPropertyAccessor> allowedPropertyAccessors, out bool restrictPropertyAccessors, out string error)
	{
		if (string.IsNullOrWhiteSpace(values))
		{
			allowedPropertyAccessors = ImmutableHashSet<ContractPropertyAccessor>.Empty;
			restrictPropertyAccessors = false;
			error = string.Empty;
			return true;
		}

		var rawValues = values!.Split(',');
		var builder = ImmutableHashSet.CreateBuilder<ContractPropertyAccessor>();
		foreach (var rawValue in rawValues)
		{
			var trimmedValue = rawValue.Trim();
			if (!ContractPropertyAccessorParser.TryParse(rawValue, out var accessor))
			{
				allowedPropertyAccessors = ImmutableHashSet<ContractPropertyAccessor>.Empty;
				restrictPropertyAccessors = false;
				error = $"ContractPolicy contains unknown property accessor '{trimmedValue}'. Supported values: {string.Join(", ", ContractPropertyAccessorParser.CanonicalOrder)}.";
				return false;
			}

			builder.Add(accessor);
		}

		allowedPropertyAccessors = builder.ToImmutable();
		restrictPropertyAccessors = true;
		error = string.Empty;
		return true;
	}

	private static string NormalizeContractTypeKind(string value)
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

