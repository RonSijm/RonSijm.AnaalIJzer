using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static void ParseClassElement(XElement classEl, LayerDefinition def, string xmlPath, Dictionary<string, MatcherRule> typeNameLayers, List<(PatternMatcher, MatcherRule)> matchers, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews)
	{
		var exceptions = ParseExceptions(classEl, xmlPath, def.Name, exceptionPolicy, exceptionDefinitions, exceptionReviews);
		var rule = CreateRule(classEl, def, exceptions, xmlPath);

		var exactName = classEl.Attribute("typeName")?.Value ?? classEl.Attribute("exactName")?.Value;
		if (!ArchitectureConfigurationMatcherReader.TryReadMatcher(classEl, MatchTarget.TypeName, out var matcher))
		{
			return;
		}

		// Only pure exact rules can use the dictionary without bypassing other conditions.
		if (exactName is not null && matcher.IsPureExactTypeName)
		{
			typeNameLayers[exactName] = rule;
			return;
		}

		matchers.Add((matcher, rule));
	}

	internal static void ParseNamespaceElement(XElement nsEl, LayerDefinition def, string xmlPath, List<(PatternMatcher, MatcherRule)> matchers, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews)
	{
		var exceptions = ParseExceptions(nsEl, xmlPath, def.Name, exceptionPolicy, exceptionDefinitions, exceptionReviews);
		var rule = CreateRule(nsEl, def, exceptions, xmlPath);

		if (ArchitectureConfigurationMatcherReader.TryReadMatcher(nsEl, MatchTarget.Namespace, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	private static void ParseAssemblyElement(XElement assemblyEl, LayerDefinition def, string xmlPath, List<(PatternMatcher, MatcherRule)> matchers, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews)
	{
		var exceptions = ParseExceptions(assemblyEl, xmlPath, def.Name, exceptionPolicy, exceptionDefinitions, exceptionReviews);
		var rule = CreateRule(assemblyEl, def, exceptions, xmlPath);

		if (ArchitectureConfigurationMatcherReader.TryReadMatcher(assemblyEl, MatchTarget.Assembly, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	internal static ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ParseTypePolicyMatchers(IEnumerable<ArchitectureConfigurationElementInput> containers, LayerDefinition scope, bool forbidden, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews)
	{
		var matchers = ImmutableArray.CreateBuilder<(PatternMatcher Matcher, MatcherRule Rule)>();
		foreach (var containerInput in containers)
		{
			var container = containerInput.Element;
			var xmlPath = containerInput.Path;
			foreach (var element in container.Elements().Where(element => element.Name.LocalName is "Class" or "Namespace"))
			{
				var target = element.Name.LocalName == "Namespace" ? MatchTarget.Namespace : MatchTarget.TypeName;
				if (!ArchitectureConfigurationMatcherReader.TryReadMatcher(element, target, out var matcher))
				{
					continue;
				}

				var definition = scope;
				if (forbidden)
				{
					var displayName = ArchitectureConfigurationMatcherReader.GetPrimaryMatcherValue(element) ?? "Forbidden";
					definition = LayerDefinition.Forbidden(displayName, element.Attribute("comment")?.Value, element.Element("Fix")?.Attribute("Rename")?.Value);
				}

				matchers.Add((matcher, CreateRule(element, definition, ParseExceptions(element, xmlPath, scope.Name, exceptionPolicy, exceptionDefinitions, exceptionReviews), xmlPath)));
			}
		}

		return matchers.ToImmutable();
	}

	private static MatcherRule CreateRule(XElement el, LayerDefinition def, ImmutableArray<ExceptionMatcher> exceptions, string xmlPath)
	{
		var line = (IXmlLineInfo)el;
		var hasInfo = line.HasLineInfo();
		return new MatcherRule(def, exceptions, hasInfo ? line.LineNumber : 0, hasInfo ? line.LinePosition : 0, xmlPath);
	}
}

