using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<PackageMatcher> ParsePackageMatchers(IEnumerable<XElement> containers, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var result = ImmutableArray.CreateBuilder<PackageMatcher>();
		foreach (var container in containers)
		{
			foreach (var element in container.Elements("Package"))
			{
				if (!TryReadPackageMatcher(element, xmlPath, issues, out var matcher))
				{
					continue;
				}

				result.Add(matcher);
			}
		}

		var finalResult = result.ToImmutable();

		return finalResult;
	}

	private static bool TryReadPackageMatcher(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out PackageMatcher matcher)
	{
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();

		if (element.Attribute("typeName")?.Value is { } typeName)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, typeName));
		}

		if (element.Attribute("exactName")?.Value is { } exactName)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, exactName));
		}

		if (element.Attribute("startsWith")?.Value is { } startsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.StartsWith, startsWith));
		}

		if (element.Attribute("endsWith")?.Value is { } endsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.EndsWith, endsWith));
		}

		if (element.Attribute("contains")?.Value is { } contains)
		{
			conditions.Add(new MatchCondition(MatchKind.Contains, contains));
		}

		if (element.Attribute("regex")?.Value is { } regex)
		{
			conditions.Add(new MatchCondition(MatchKind.Regex, regex));
		}

		if (conditions.Count == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Package requires at least one matcher attribute.", element, xmlPath);
			matcher = default;
			return false;
		}

		var lineInfo = (IXmlLineInfo)element;
		matcher = new PackageMatcher(
			conditions.ToImmutable(),
			element.Attribute("comment")?.Value,
			element.Attribute("description")?.Value,
			xmlPath,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);

		return true;
	}

	private static bool TryReadProjectMatcher(XElement element, out ProjectMatcher matcher)
	{
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();

		if (element.Attribute("typeName")?.Value is { } typeName)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, typeName));
		}

		if (element.Attribute("exactName")?.Value is { } exactName)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, exactName));
		}

		if (element.Attribute("endsWith")?.Value is { } endsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.EndsWith, endsWith));
		}

		if (element.Attribute("startsWith")?.Value is { } startsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.StartsWith, startsWith));
		}

		if (element.Attribute("contains")?.Value is { } contains)
		{
			conditions.Add(new MatchCondition(MatchKind.Contains, contains));
		}

		if (element.Attribute("regex")?.Value is { } regex)
		{
			conditions.Add(new MatchCondition(MatchKind.Regex, regex));
		}

		matcher = new ProjectMatcher(conditions.ToImmutable());
		var result = conditions.Count > 0;

		return result;
	}
}

