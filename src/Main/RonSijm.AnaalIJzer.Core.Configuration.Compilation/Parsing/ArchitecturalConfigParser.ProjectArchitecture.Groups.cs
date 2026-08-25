using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<ProjectGroup> ParseProjectGroups(XElement root, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var result = ImmutableArray.CreateBuilder<ProjectGroup>();
		var seenNames = new HashSet<string>(StringComparer.Ordinal);

		foreach (var element in root.Elements("ProjectGroup"))
		{
			var name = element.Attribute("name")?.Value;
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			if (string.Equals(name, "*", StringComparison.Ordinal))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ProjectGroup name '*' is reserved and may not be used.", element, xmlPath);
				continue;
			}

			var normalizedName = name!;
			if (!seenNames.Add(normalizedName))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"ProjectGroup '{normalizedName}' is declared more than once.", element, xmlPath);
				continue;
			}

			var matchers = ImmutableArray.CreateBuilder<ProjectMatcher>();
			foreach (var projectElement in element.Elements("Project"))
			{
				if (TryReadProjectMatcher(projectElement, out var matcher))
				{
					matchers.Add(matcher);
				}
			}

			if (matchers.Count == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"ProjectGroup '{normalizedName}' does not contain a Project matcher.", element, xmlPath);
				continue;
			}

			var lineInfo = (IXmlLineInfo)element;
			result.Add(new ProjectGroup(normalizedName, matchers.ToImmutable(), element.Attribute("description")?.Value, xmlPath, lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0, lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
		}

		var finalResult = result.ToImmutable();

		return finalResult;
	}
}

