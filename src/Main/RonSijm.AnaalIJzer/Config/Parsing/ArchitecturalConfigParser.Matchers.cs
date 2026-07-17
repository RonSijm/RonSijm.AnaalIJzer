using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;

namespace RonSijm.AnaalIJzer.Parsing;

internal static partial class ArchitecturalConfigParser
{
	private static void ParseClassElement(XElement classEl, LayerDefinition def, string xmlPath, Dictionary<string, MatcherRule> typeNameLayers, List<(PatternMatcher, MatcherRule)> matchers)
	{
		var exceptions = ParseExceptions(classEl);
		var rule = CreateRule(classEl, def, exceptions, xmlPath);

		var exactName = classEl.Attribute("typeName")?.Value ?? classEl.Attribute("exactName")?.Value;
		if (!TryReadMatcher(classEl, MatchTarget.TypeName, out var matcher))
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

	private static void ParseNamespaceElement(XElement nsEl, LayerDefinition def, string xmlPath, List<(PatternMatcher, MatcherRule)> matchers)
	{
		var exceptions = ParseExceptions(nsEl);
		var rule = CreateRule(nsEl, def, exceptions, xmlPath);

		if (TryReadMatcher(nsEl, MatchTarget.Namespace, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	private static void ParseAssemblyElement(XElement assemblyEl, LayerDefinition def, string xmlPath, List<(PatternMatcher, MatcherRule)> matchers)
	{
		var exceptions = ParseExceptions(assemblyEl);
		var rule = CreateRule(assemblyEl, def, exceptions, xmlPath);

		if (TryReadMatcher(assemblyEl, MatchTarget.Assembly, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	private static ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ParseTypePolicyMatchers(IEnumerable<(XElement Element, string Path)> containers, LayerDefinition scope, bool forbidden)
	{
		var matchers = ImmutableArray.CreateBuilder<(PatternMatcher Matcher, MatcherRule Rule)>();
		foreach (var (container, xmlPath) in containers)
		{
			foreach (var element in container.Elements().Where(element => element.Name.LocalName is "Class" or "Namespace"))
			{
				var target = element.Name.LocalName == "Namespace" ? MatchTarget.Namespace : MatchTarget.TypeName;
				if (!TryReadMatcher(element, target, out var matcher))
				{
					continue;
				}

				var definition = scope;
				if (forbidden)
				{
					var displayName = GetForbiddenDisplayName(element) ?? "Forbidden";
					definition = LayerDefinition.Forbidden(displayName, element.Attribute("comment")?.Value, element.Element("Fix")?.Attribute("Rename")?.Value);
				}

				matchers.Add((matcher, CreateRule(element, definition, ParseExceptions(element), xmlPath)));
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

	/// <summary>
	///     Parses any <c>&lt;Exceptions&gt;</c> child of <paramref name="ruleEl" /> into a tree.
	///     Nested exceptions flip the previous exception level again, with the deepest matching
	///     level winning.
	/// </summary>
	private static ImmutableArray<ExceptionMatcher> ParseExceptions(XElement ruleEl)
	{
		var exceptionsContainer = ruleEl.Element("Exceptions");
		if (exceptionsContainer is null)
		{
			return ImmutableArray<ExceptionMatcher>.Empty;
		}

		var builder = ImmutableArray.CreateBuilder<ExceptionMatcher>();

		foreach (var exEl in exceptionsContainer.Elements("Class"))
		{
			if (TryReadMatcher(exEl, MatchTarget.TypeName, out var m))
			{
				builder.Add(new ExceptionMatcher(m, ParseExceptions(exEl)));
			}
		}

		foreach (var exEl in exceptionsContainer.Elements("Namespace"))
		{
			if (TryReadMatcher(exEl, MatchTarget.Namespace, out var m))
			{
				builder.Add(new ExceptionMatcher(m, ParseExceptions(exEl)));
			}
		}

		return builder.ToImmutable();
	}

	/// <summary>
	///     Collects every matcher attribute on <paramref name="el" />. Conditions on one
	///     element are conjunctive; separate elements remain alternatives.
	/// </summary>
	private static bool TryReadMatcher(XElement el, MatchTarget target, out PatternMatcher matcher)
	{
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		if (target == MatchTarget.TypeName)
		{
			if (el.Attribute("typeName")?.Value is { } typeName)
			{
				conditions.Add(new MatchCondition(MatchKind.Equals, typeName));
			}

			if (el.Attribute("exactName")?.Value is { } exactName)
			{
				conditions.Add(new MatchCondition(MatchKind.Equals, exactName));
			}

			if (el.Attribute("exactFullName")?.Value is { } exactFullName)
			{
				conditions.Add(new MatchCondition(MatchKind.EqualsFullName, exactFullName));
			}

			if (el.Attribute("inherits")?.Value is { } inherits)
			{
				conditions.Add(new MatchCondition(MatchKind.Inherits, inherits));
			}

			if (el.Attribute("implements")?.Value is { } implements)
			{
				conditions.Add(new MatchCondition(MatchKind.Implements, implements));
			}

			if (el.Attribute("withAttribute")?.Value is { } withAttribute)
			{
				conditions.Add(new MatchCondition(MatchKind.HasAttribute, withAttribute));
			}

			if (el.Attribute("withAccessModifier")?.Value is { } withAccessModifier)
			{
				conditions.Add(new MatchCondition(MatchKind.HasAccessModifier, withAccessModifier));
			}

			if (el.Attribute("typeKind")?.Value is { } typeKind)
			{
				conditions.Add(new MatchCondition(MatchKind.HasTypeKind, typeKind));
			}
		}
		else if (el.Attribute("exactName")?.Value is { } exactValue)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, exactValue));
		}

		if (el.Attribute("endsWith")?.Value is { } endsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.EndsWith, endsWith));
		}

		if (el.Attribute("startsWith")?.Value is { } startsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.StartsWith, startsWith));
		}

		if (el.Attribute("contains")?.Value is { } contains)
		{
			conditions.Add(new MatchCondition(MatchKind.Contains, contains));
		}

		if (el.Attribute("regex")?.Value is { } regex)
		{
			conditions.Add(new MatchCondition(MatchKind.Regex, regex));
		}

		matcher = new PatternMatcher(target, conditions.ToImmutable());
		return conditions.Count > 0;
	}

	/// <summary>
	///     Best-effort display label for a forbidden rule, used in diagnostics, reports, and the
	///     architecture documentation. Falls back to a generic placeholder when no matcher attribute is
	///     present (the rule will then never match anything anyway).
	/// </summary>
	private static string? GetForbiddenDisplayName(XElement el)
	{
		var result = el.Attribute("typeName")?.Value
		             ?? el.Attribute("exactName")?.Value
		             ?? el.Attribute("exactFullName")?.Value
		             ?? el.Attribute("inherits")?.Value
		             ?? el.Attribute("implements")?.Value
		             ?? el.Attribute("withAttribute")?.Value
		             ?? el.Attribute("withAccessModifier")?.Value
		             ?? el.Attribute("endsWith")?.Value
		             ?? el.Attribute("startsWith")?.Value
		             ?? el.Attribute("contains")?.Value
		             ?? el.Attribute("regex")?.Value;

		return result;
	}
}
