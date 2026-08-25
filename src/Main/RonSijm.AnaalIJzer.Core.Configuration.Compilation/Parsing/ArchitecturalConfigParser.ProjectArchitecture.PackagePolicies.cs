using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<PackagePolicy> ParsePackagePolicies(XElement root, string xmlPath, ImmutableArray<ProjectGroup> groups, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var groupNames = new HashSet<string>(groups.Select(group => group.Name), StringComparer.Ordinal);
		var result = ImmutableArray.CreateBuilder<PackagePolicy>();

		foreach (var element in root.Elements("PackagePolicy"))
		{
			var projectGroup = element.Attribute("projectGroup")?.Value;
			if (string.IsNullOrWhiteSpace(projectGroup))
			{
				continue;
			}

			var normalizedProjectGroup = projectGroup!;

			if (!groupNames.Contains(normalizedProjectGroup))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"PackagePolicy references unknown project group '{normalizedProjectGroup}'.", element, xmlPath);
				continue;
			}

			if (!TryReadBooleanAttribute(element, "includeTransitive", out var includeTransitive))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "PackagePolicy contains an invalid includeTransitive value. Use true, false, 1, or 0.", element, xmlPath);
				continue;
			}

			var allowedMatchers = ParsePackageMatchers(element.Elements("Allowed"), xmlPath, issues);
			var forbiddenMatchers = ParsePackageMatchers(element.Elements("Forbidden"), xmlPath, issues);
			if (allowedMatchers.IsDefaultOrEmpty && forbiddenMatchers.IsDefaultOrEmpty)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"PackagePolicy for project group '{normalizedProjectGroup}' requires at least one Allowed or Forbidden Package matcher.", element, xmlPath);
				continue;
			}

			var lineInfo = (IXmlLineInfo)element;
			result.Add(new PackagePolicy(
				normalizedProjectGroup,
				allowedMatchers,
				forbiddenMatchers,
				includeTransitive,
				element.Attribute("description")?.Value,
				xmlPath,
				lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
				lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
		}

		var finalResult = result.ToImmutable();

		return finalResult;
	}
}

