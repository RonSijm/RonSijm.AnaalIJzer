using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<ProjectReferenceRule> ParseProjectReferenceRules(XElement root, string xmlPath, ImmutableArray<ProjectGroup> groups, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var groupNames = new HashSet<string>(groups.Select(group => group.Name), StringComparer.Ordinal);
		var result = ImmutableArray.CreateBuilder<ProjectReferenceRule>();

		foreach (var element in root.Elements().Where(element => element.Name.LocalName is "AllowedProjectReference" or "BlockedProjectReference"))
		{
			var from = element.Attribute("from")?.Value;
			var to = element.Attribute("to")?.Value;
			if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
			{
				continue;
			}

			var normalizedFrom = from!;
			var normalizedTo = to!;

			if (element.Attribute("allowedSites") is not null || element.Attribute("blockedSites") is not null || element.Attribute("appliesToDescendants") is not null)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} does not support allowedSites, blockedSites, or appliesToDescendants.", element, xmlPath);
				continue;
			}

			if (normalizedFrom != "*" && !groupNames.Contains(normalizedFrom))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} references unknown source project group '{normalizedFrom}'.", element, xmlPath);
				continue;
			}

			if (normalizedTo != "*" && !groupNames.Contains(normalizedTo))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} references unknown target project group '{normalizedTo}'.", element, xmlPath);
				continue;
			}

			var lineInfo = (IXmlLineInfo)element;
			var kind = element.Name.LocalName == "BlockedProjectReference" ? ProjectReferenceRuleKind.Blocked : ProjectReferenceRuleKind.Allowed;
			result.Add(new ProjectReferenceRule(kind, normalizedFrom, normalizedTo, element.Attribute("description")?.Value, xmlPath, lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0, lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
		}

		var finalResult = result.ToImmutable();

		return finalResult;
	}
}

