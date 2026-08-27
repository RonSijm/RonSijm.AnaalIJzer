using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static ProjectArchitectureConfig ParseProjectArchitecture(IEnumerable<ArchitectureConfigurationElementInput> elements, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var root = elements.FirstOrDefault(item => item.Element.Name.LocalName == "ProjectArchitecture");
		if (root.Element is null)
		{
			return ProjectArchitectureConfig.Empty;
		}

		if (!TryReadBooleanAttribute(root.Element, "requireRecognizedProjects", out var requireRecognizedProjects))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ProjectArchitecture contains an invalid requireRecognizedProjects value. Use true, false, 1, or 0.", root.Element, root.Path);
			return ProjectArchitectureConfig.Empty;
		}

		var groups = ParseProjectGroups(root.Element, root.Path, issues);
		var rules = ParseProjectReferenceRules(root.Element, root.Path, groups, issues);
		var packagePolicies = ParsePackagePolicies(root.Element, root.Path, groups, issues);
		var result = new ProjectArchitectureConfig(groups, rules, packagePolicies, requireRecognizedProjects);

		return result;
	}
}

