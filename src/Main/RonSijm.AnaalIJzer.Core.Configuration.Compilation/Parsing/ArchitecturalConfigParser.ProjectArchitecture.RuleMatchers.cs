using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static bool TryReadProjectReferenceRuleMatchers(
		XElement ruleElement,
		string selectorName,
		string xmlPath,
		ImmutableArray<ConfigurationIssue>.Builder issues,
		out ImmutableArray<ProjectMatcher> matchers)
	{
		var builder = ImmutableArray.CreateBuilder<ProjectMatcher>();
		foreach (var selectorElement in ruleElement.Elements(selectorName))
		{
			if (!TryReadProjectMatcher(selectorElement, out var matcher))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{ruleElement.Name.LocalName} {selectorName} selector requires at least one project matcher attribute.", selectorElement, xmlPath);
				matchers = ImmutableArray<ProjectMatcher>.Empty;

				return false;
			}

			builder.Add(matcher);
		}

		matchers = builder.ToImmutable();

		return true;
	}

	private static bool HasOnlyProjectReferenceRuleSelectorChildren(XElement ruleElement, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		foreach (var child in ruleElement.Elements())
		{
			if (child.Name.LocalName is "From" or "To")
			{
				continue;
			}

			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{ruleElement.Name.LocalName} supports only From and To selector children.", child, xmlPath);

			return false;
		}

		return true;
	}
}
