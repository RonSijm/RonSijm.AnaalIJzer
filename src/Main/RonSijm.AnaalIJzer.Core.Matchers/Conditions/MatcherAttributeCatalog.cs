using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Matchers.Conditions;

public static class MatcherAttributeCatalog
{
	private static readonly ImmutableArray<MatcherAttributeDefinition> Definitions =
	[
		new(MatcherAttributeId.TypeName, "typeName", MatchKind.Equals),
		new(MatcherAttributeId.ExactName, "exactName", MatchKind.Equals),
		new(MatcherAttributeId.ExactFullName, "exactFullName", MatchKind.EqualsFullName),
		new(MatcherAttributeId.Inherits, "inherits", MatchKind.Inherits),
		new(MatcherAttributeId.Implements, "implements", MatchKind.Implements),
		new(MatcherAttributeId.WithAttribute, "withAttribute", MatchKind.HasAttribute),
		new(MatcherAttributeId.WithAccessModifier, "withAccessModifier", MatchKind.HasAccessModifier),
		new(MatcherAttributeId.TypeKind, "typeKind", MatchKind.HasTypeKind),
		new(MatcherAttributeId.EndsWith, "endsWith", MatchKind.EndsWith),
		new(MatcherAttributeId.StartsWith, "startsWith", MatchKind.StartsWith),
		new(MatcherAttributeId.Contains, "contains", MatchKind.Contains),
		new(MatcherAttributeId.Regex, "regex", MatchKind.Regex),
		new(MatcherAttributeId.Value, "value", MatchKind.Equals)
	];

	private static readonly ImmutableArray<MatcherAttributeId> TypeAttributes =
	[
		MatcherAttributeId.TypeName,
		MatcherAttributeId.ExactName,
		MatcherAttributeId.ExactFullName,
		MatcherAttributeId.Inherits,
		MatcherAttributeId.Implements,
		MatcherAttributeId.WithAttribute,
		MatcherAttributeId.WithAccessModifier,
		MatcherAttributeId.TypeKind,
		MatcherAttributeId.EndsWith,
		MatcherAttributeId.StartsWith,
		MatcherAttributeId.Contains,
		MatcherAttributeId.Regex
	];

	private static readonly ImmutableArray<MatcherAttributeId> NamespaceOrAssemblyAttributes =
	[
		MatcherAttributeId.ExactName,
		MatcherAttributeId.EndsWith,
		MatcherAttributeId.StartsWith,
		MatcherAttributeId.Contains,
		MatcherAttributeId.Regex
	];

	private static readonly ImmutableArray<MatcherAttributeId> CodeObservationAttributes =
	[
		MatcherAttributeId.TypeName,
		MatcherAttributeId.ExactName,
		MatcherAttributeId.ExactFullName,
		MatcherAttributeId.EndsWith,
		MatcherAttributeId.StartsWith,
		MatcherAttributeId.Contains,
		MatcherAttributeId.Regex
	];

	private static readonly ImmutableArray<MatcherAttributeId> SemanticCodeObservationAttributes =
	[
		MatcherAttributeId.TypeName,
		MatcherAttributeId.ExactName,
		MatcherAttributeId.ExactFullName,
		MatcherAttributeId.EndsWith,
		MatcherAttributeId.StartsWith,
		MatcherAttributeId.Contains,
		MatcherAttributeId.Regex,
		MatcherAttributeId.Inherits,
		MatcherAttributeId.Implements,
		MatcherAttributeId.WithAttribute,
		MatcherAttributeId.WithAccessModifier,
		MatcherAttributeId.TypeKind
	];

	private static readonly ImmutableArray<MatcherAttributeId> ProjectOrPackageAttributes =
	[
		MatcherAttributeId.TypeName,
		MatcherAttributeId.ExactName,
		MatcherAttributeId.StartsWith,
		MatcherAttributeId.EndsWith,
		MatcherAttributeId.Contains,
		MatcherAttributeId.Regex
	];

	private static readonly ImmutableArray<MatcherAttributeId> PrimaryAttributes =
	[
		MatcherAttributeId.TypeName,
		MatcherAttributeId.Value,
		MatcherAttributeId.ExactName,
		MatcherAttributeId.ExactFullName,
		MatcherAttributeId.Inherits,
		MatcherAttributeId.Implements,
		MatcherAttributeId.WithAttribute,
		MatcherAttributeId.WithAccessModifier,
		MatcherAttributeId.TypeKind,
		MatcherAttributeId.EndsWith,
		MatcherAttributeId.StartsWith,
		MatcherAttributeId.Contains,
		MatcherAttributeId.Regex
	];

	public static ImmutableArray<string> PrimaryAttributeNames { get; } = GetAttributeNames(PrimaryAttributes);

	public static ImmutableArray<MatchCondition> CreateConditions(Func<string, string?> getAttributeValue, MatcherAttributeProfile profile, bool includeLiteralValue = false)
	{
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		foreach (var attribute in GetProfileAttributes(profile))
		{
			AddCondition(getAttributeValue, attribute, GetOperand(profile, attribute), conditions);
		}

		if (includeLiteralValue)
		{
			AddCondition(getAttributeValue, MatcherAttributeId.Value, MatchOperand.Declaration, conditions);
		}

		var result = conditions.ToImmutable();

		return result;
	}

	public static ImmutableArray<string> GetAttributeNames(MatcherAttributeProfile profile, bool includeLiteralValue = false)
	{
		var attributes = GetProfileAttributes(profile);
		if (includeLiteralValue)
		{
			attributes = attributes.Add(MatcherAttributeId.Value);
		}

		var result = GetAttributeNames(attributes);

		return result;
	}

	public static bool IsMatcherAttribute(string attributeName)
	{
		var result = GetProfileAttributes(MatcherAttributeProfile.Type)
			.Any(attribute => IsAttributeNamed(attribute, attributeName));

		return result;
	}

	public static bool IsDisplayAttribute(string attributeName)
	{
		var result = IsMatcherAttribute(attributeName) || IsAttributeNamed(MatcherAttributeId.Value, attributeName);

		return result;
	}

	public static bool IsSupportedAttribute(string attributeName, MatcherAttributeProfile profile, bool includeLiteralValue = false)
	{
		var result = includeLiteralValue && IsAttributeNamed(MatcherAttributeId.Value, attributeName)
			|| GetProfileAttributes(profile).Any(attribute => IsAttributeNamed(attribute, attributeName));

		return result;
	}

	private static void AddCondition(Func<string, string?> getAttributeValue, MatcherAttributeId attribute, MatchOperand operand, ImmutableArray<MatchCondition>.Builder conditions)
	{
		var definition = GetDefinition(attribute);
		if (getAttributeValue(definition.Name) is not { } value)
		{
			return;
		}

		conditions.Add(new MatchCondition(definition.Kind, value, operand));
	}

	private static ImmutableArray<string> GetAttributeNames(ImmutableArray<MatcherAttributeId> attributes)
	{
		var names = ImmutableArray.CreateBuilder<string>();
		foreach (var attribute in attributes)
		{
			names.Add(GetDefinition(attribute).Name);
		}

		var result = names.ToImmutable();

		return result;
	}

	private static bool IsAttributeNamed(MatcherAttributeId attribute, string name)
	{
		var result = string.Equals(GetDefinition(attribute).Name, name, StringComparison.Ordinal);

		return result;
	}

	private static MatcherAttributeDefinition GetDefinition(MatcherAttributeId attribute)
	{
		var result = Definitions.Single(definition => definition.Id == attribute);

		return result;
	}

	private static ImmutableArray<MatcherAttributeId> GetProfileAttributes(MatcherAttributeProfile profile)
	{
		var result = profile switch
		{
			MatcherAttributeProfile.Type => TypeAttributes,
			MatcherAttributeProfile.NamespaceOrAssembly => NamespaceOrAssemblyAttributes,
			MatcherAttributeProfile.Declaration => TypeAttributes,
			MatcherAttributeProfile.CodeObservation => CodeObservationAttributes,
			MatcherAttributeProfile.SemanticCodeObservation => SemanticCodeObservationAttributes,
			MatcherAttributeProfile.ProjectOrPackage => ProjectOrPackageAttributes,
			_ => ImmutableArray<MatcherAttributeId>.Empty
		};

		return result;
	}

	private static MatchOperand GetOperand(MatcherAttributeProfile profile, MatcherAttributeId attribute)
	{
		var result = profile is MatcherAttributeProfile.Type or MatcherAttributeProfile.NamespaceOrAssembly or MatcherAttributeProfile.ProjectOrPackage
			? MatchOperand.Subject
			: attribute is MatcherAttributeId.TypeName or MatcherAttributeId.ExactFullName or MatcherAttributeId.Inherits or MatcherAttributeId.Implements or MatcherAttributeId.TypeKind
				? MatchOperand.AssociatedType
				: MatchOperand.Declaration;

		return result;
	}

	private enum MatcherAttributeId
	{
		TypeName,
		ExactName,
		ExactFullName,
		Inherits,
		Implements,
		WithAttribute,
		WithAccessModifier,
		TypeKind,
		EndsWith,
		StartsWith,
		Contains,
		Regex,
		Value
	}

	private readonly struct MatcherAttributeDefinition(MatcherAttributeId id, string name, MatchKind kind)
	{
		public MatcherAttributeId Id { get; } = id;

		public string Name { get; } = name;

		public MatchKind Kind { get; } = kind;
	}
}
