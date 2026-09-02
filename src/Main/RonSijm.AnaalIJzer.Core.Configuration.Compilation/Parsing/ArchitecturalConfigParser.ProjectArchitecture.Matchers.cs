using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

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
		var conditions = MatcherAttributeCatalog.CreateConditions(
			attributeName => element.Attribute(attributeName)?.Value,
			MatcherAttributeProfile.ProjectOrPackage);

		if (conditions.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Package requires at least one matcher attribute.", element, xmlPath);
			matcher = default;
			return false;
		}

		var lineInfo = (IXmlLineInfo)element;
		matcher = new PackageMatcher(
			conditions,
			element.Attribute("comment")?.Value,
			element.Attribute("description")?.Value,
			xmlPath,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);

		return true;
	}

	private static bool TryReadProjectMatcher(XElement element, out ProjectMatcher matcher)
	{
		var conditions = MatcherAttributeCatalog.CreateConditions(
			attributeName => element.Attribute(attributeName)?.Value,
			MatcherAttributeProfile.ProjectOrPackage);

		matcher = new ProjectMatcher(conditions);
		var result = conditions.Length > 0;

		return result;
	}
}

