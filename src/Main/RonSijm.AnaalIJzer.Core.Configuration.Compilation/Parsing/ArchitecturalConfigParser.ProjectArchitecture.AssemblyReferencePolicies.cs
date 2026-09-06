using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<AssemblyReferencePolicy> ParseAssemblyReferencePolicies(XElement root, string xmlPath, ImmutableArray<ProjectGroup> groups, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var groupNames = new HashSet<string>(groups.Select(group => group.Name), StringComparer.Ordinal);
		var result = ImmutableArray.CreateBuilder<AssemblyReferencePolicy>();

		foreach (var element in root.Elements("AssemblyReferencePolicy"))
		{
			var projectGroup = element.Attribute("projectGroup")?.Value;
			if (string.IsNullOrWhiteSpace(projectGroup))
			{
				continue;
			}

			var normalizedProjectGroup = projectGroup!;
			if (!groupNames.Contains(normalizedProjectGroup))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"AssemblyReferencePolicy references unknown project group '{normalizedProjectGroup}'.", element, xmlPath);
				continue;
			}

			var allowedMatchers = ParseReferenceIdentityMatchers(element.Elements("Allowed"), "Assembly", xmlPath, issues);
			var forbiddenMatchers = ParseReferenceIdentityMatchers(element.Elements("Forbidden"), "Assembly", xmlPath, issues);
			if (allowedMatchers.IsDefaultOrEmpty && forbiddenMatchers.IsDefaultOrEmpty)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"AssemblyReferencePolicy for project group '{normalizedProjectGroup}' requires at least one Allowed or Forbidden Assembly matcher.", element, xmlPath);
				continue;
			}

			var lineInfo = (IXmlLineInfo)element;
			result.Add(new AssemblyReferencePolicy(
				normalizedProjectGroup,
				allowedMatchers,
				forbiddenMatchers,
				element.Attribute("description")?.Value,
				xmlPath,
				lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
				lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
		}

		var finalResult = result.ToImmutable();

		return finalResult;
	}
}
