using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Declarations;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

public static class ArchitectureConfigurationMatcherReader
{
	public static bool TryReadMatcher(XElement element, MatchTarget target, out PatternMatcher matcher)
	{
		var conditions = ReadMatcherConditions(element, target);
		var declarationMatchers = target == MatchTarget.TypeName
			? ReadDeclarationMatchers(element)
			: ImmutableArray<DeclarationMatcher>.Empty;
		matcher = new PatternMatcher(target, conditions, declarationMatchers);

		return conditions.Length > 0;
	}

	public static bool TryReadDeclarationMatcher(XElement element, out DeclarationMatcher matcher)
	{
		if (!DeclarationMatchTargetParser.TryParse(element.Name.LocalName, out var target))
		{
			matcher = default;

			return false;
		}

		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		TryAddCondition(element, "typeName", MatchKind.Equals, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "exactName", MatchKind.Equals, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "exactFullName", MatchKind.EqualsFullName, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "inherits", MatchKind.Inherits, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "implements", MatchKind.Implements, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "withAttribute", MatchKind.HasAttribute, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "withAccessModifier", MatchKind.HasAccessModifier, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "typeKind", MatchKind.HasTypeKind, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "endsWith", MatchKind.EndsWith, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "startsWith", MatchKind.StartsWith, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "contains", MatchKind.Contains, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "regex", MatchKind.Regex, conditions, MatchOperand.Declaration);
		var observationMatchers = ReadCodeObservationMatchers(element);

		matcher = new DeclarationMatcher(target, conditions.ToImmutable(), observationMatchers);
		var result = conditions.Count > 0;

		return result;
	}

	public static bool TryReadCodeObservationMatcher(XElement element, out CodeObservationMatcher matcher)
	{
		if (!CodeObservationMatchTargetParser.TryParse(element.Name.LocalName, out var target))
		{
			matcher = default;

			return false;
		}

		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		TryAddCondition(element, "typeName", MatchKind.Equals, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "exactName", MatchKind.Equals, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "exactFullName", MatchKind.EqualsFullName, conditions, MatchOperand.AssociatedType);
		TryAddCondition(element, "endsWith", MatchKind.EndsWith, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "startsWith", MatchKind.StartsWith, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "contains", MatchKind.Contains, conditions, MatchOperand.Declaration);
		TryAddCondition(element, "regex", MatchKind.Regex, conditions, MatchOperand.Declaration);

		matcher = new CodeObservationMatcher(target, conditions.ToImmutable());

		return true;
	}

	public static string? GetMatcherDisplayName(XElement element)
	{
		var attributes = element.Attributes()
			.Where(attribute => IsMatcherAttribute(attribute.Name.LocalName))
			.Select(attribute => $"{attribute.Name.LocalName}=\"{attribute.Value}\"")
			.ToArray();
		var result = attributes.Length == 0 ? null : string.Join(" ", attributes);

		return result;
	}

	public static string? GetPrimaryMatcherValue(XElement element)
	{
		var result = element.Attribute("typeName")?.Value
		             ?? element.Attribute("exactName")?.Value
		             ?? element.Attribute("exactFullName")?.Value
		             ?? element.Attribute("inherits")?.Value
		             ?? element.Attribute("implements")?.Value
		             ?? element.Attribute("withAttribute")?.Value
		             ?? element.Attribute("withAccessModifier")?.Value
		             ?? element.Attribute("typeKind")?.Value
		             ?? element.Attribute("endsWith")?.Value
		             ?? element.Attribute("startsWith")?.Value
		             ?? element.Attribute("contains")?.Value
		             ?? element.Attribute("regex")?.Value;

		return result;
	}

	private static bool IsMatcherAttribute(string name)
	{
		var result = name is
			"typeName" or "exactName" or "exactFullName" or "inherits" or "implements" or "withAttribute" or
			"withAccessModifier" or "typeKind" or "endsWith" or "startsWith" or "contains" or "regex";

		return result;
	}

	private static ImmutableArray<MatchCondition> ReadMatcherConditions(XElement element, MatchTarget target)
	{
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		if (target == MatchTarget.TypeName)
		{
			TryAddCondition(element, "typeName", MatchKind.Equals, conditions);
			TryAddCondition(element, "exactName", MatchKind.Equals, conditions);
			TryAddCondition(element, "exactFullName", MatchKind.EqualsFullName, conditions);
			TryAddCondition(element, "inherits", MatchKind.Inherits, conditions);
			TryAddCondition(element, "implements", MatchKind.Implements, conditions);
			TryAddCondition(element, "withAttribute", MatchKind.HasAttribute, conditions);
			TryAddCondition(element, "withAccessModifier", MatchKind.HasAccessModifier, conditions);
			TryAddCondition(element, "typeKind", MatchKind.HasTypeKind, conditions);
		}
		else
		{
			TryAddCondition(element, "exactName", MatchKind.Equals, conditions);
		}

		TryAddCondition(element, "endsWith", MatchKind.EndsWith, conditions);
		TryAddCondition(element, "startsWith", MatchKind.StartsWith, conditions);
		TryAddCondition(element, "contains", MatchKind.Contains, conditions);
		TryAddCondition(element, "regex", MatchKind.Regex, conditions);

		var result = conditions.ToImmutable();

		return result;
	}

	private static ImmutableArray<DeclarationMatcher> ReadDeclarationMatchers(XElement element)
	{
		var matchers = ImmutableArray.CreateBuilder<DeclarationMatcher>();
		foreach (var child in element.Elements())
		{
			if (TryReadDeclarationMatcher(child, out var matcher))
			{
				matchers.Add(matcher);
			}
		}

		var result = matchers.ToImmutable();

		return result;
	}

	private static ImmutableArray<CodeObservationMatcher> ReadCodeObservationMatchers(XElement element)
	{
		var matchers = ImmutableArray.CreateBuilder<CodeObservationMatcher>();
		foreach (var child in element.Elements())
		{
			if (TryReadCodeObservationMatcher(child, out var matcher))
			{
				matchers.Add(matcher);
			}
		}

		var result = matchers.ToImmutable();

		return result;
	}

	private static void TryAddCondition(XElement element, string attributeName, MatchKind kind, ImmutableArray<MatchCondition>.Builder conditions, MatchOperand operand = MatchOperand.Subject)
	{
		if (element.Attribute(attributeName)?.Value is not { } value)
		{
			return;
		}

		conditions.Add(new MatchCondition(kind, value, operand));
	}
}
