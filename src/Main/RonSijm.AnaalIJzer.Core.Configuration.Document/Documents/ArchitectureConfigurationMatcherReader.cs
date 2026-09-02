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

		var conditions = MatcherAttributeCatalog.CreateConditions(
			attributeName => element.Attribute(attributeName)?.Value,
			MatcherAttributeProfile.Declaration);
		var observationMatchers = ReadCodeObservationMatchers(element);

		matcher = new DeclarationMatcher(target, conditions, observationMatchers);
		var result = conditions.Length > 0;

		return result;
	}

	public static bool TryReadCodeObservationMatcher(XElement element, out CodeObservationMatcher matcher)
	{
		var result = TryReadCodeObservationMatcher(element, false, out matcher);

		return result;
	}

	public static bool TryReadCodeObservationMatcher(XElement element, bool allowSemanticConditions, out CodeObservationMatcher matcher)
	{
		if (!CodeObservationMatchTargetParser.TryParse(element.Name.LocalName, out var target))
		{
			matcher = default;

			return false;
		}

		var profile = allowSemanticConditions
			? MatcherAttributeProfile.SemanticCodeObservation
			: MatcherAttributeProfile.CodeObservation;
		var conditions = MatcherAttributeCatalog.CreateConditions(
			attributeName => element.Attribute(attributeName)?.Value,
			profile,
			target == CodeObservationMatchTarget.Literal);

		matcher = new CodeObservationMatcher(target, conditions);

		return true;
	}

	public static string? GetMatcherDisplayName(XElement element)
	{
		var attributes = element.Attributes()
			.Where(attribute => MatcherAttributeCatalog.IsDisplayAttribute(attribute.Name.LocalName))
			.Select(attribute => $"{attribute.Name.LocalName}=\"{attribute.Value}\"")
			.ToArray();
		var result = attributes.Length == 0 ? null : string.Join(" ", attributes);

		return result;
	}

	public static string? GetPrimaryMatcherValue(XElement element)
	{
		foreach (var attributeName in MatcherAttributeCatalog.PrimaryAttributeNames)
		{
			if (element.Attribute(attributeName)?.Value is { } value)
			{
				return value;
			}
		}

		return null;
	}

	private static ImmutableArray<MatchCondition> ReadMatcherConditions(XElement element, MatchTarget target)
	{
		var profile = target == MatchTarget.TypeName
			? MatcherAttributeProfile.Type
			: MatcherAttributeProfile.NamespaceOrAssembly;
		var result = MatcherAttributeCatalog.CreateConditions(attributeName => element.Attribute(attributeName)?.Value, profile);

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

}
