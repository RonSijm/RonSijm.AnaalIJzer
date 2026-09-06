using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public static class ProjectReferenceEvaluator
{
	public static ProjectReferenceEvaluation Evaluate(ProjectArchitectureConfig config, string sourceProjectName, string targetProjectName)
	{
		var sourceGroup = MatchProjectGroup(config.ProjectGroups, sourceProjectName);
		var targetGroup = MatchProjectGroup(config.ProjectGroups, targetProjectName);
		var sourceGroupName = sourceGroup.HasValue ? sourceGroup.Value.Name : null;
		var targetGroupName = targetGroup.HasValue ? targetGroup.Value.Name : null;

		if (config.RequireRecognizedProjects && sourceGroup is null)
		{
			var result = new ProjectReferenceEvaluation(false, $"source project '{sourceProjectName}' is not assigned to a configured ProjectGroup", null, null, targetGroupName);

			return result;
		}

		if (config.RequireRecognizedProjects && targetGroup is null)
		{
			var result = new ProjectReferenceEvaluation(false, $"target project '{targetProjectName}' is not assigned to a configured ProjectGroup", null, sourceGroupName, null);

			return result;
		}

		if (sourceGroup is null || targetGroup is null)
		{
			var blockOnlyResult = EvaluateWithoutAllowlist(sourceGroup, targetGroup);

			return blockOnlyResult;
		}

		var blockedRule = FindMatchingRule(config.Rules, sourceGroupName!, targetGroupName!, sourceProjectName, targetProjectName, ProjectReferenceRuleKind.Blocked);
		if (blockedRule is { } blocked)
		{
			var result = new ProjectReferenceEvaluation(false, $"BlockedProjectReference from '{blocked.From}' to '{blocked.To}' denies this project edge", blocked, sourceGroupName, targetGroupName);

			return result;
		}

		var sourceAllowedRules = config.Rules
			.Where(rule => rule.Kind == ProjectReferenceRuleKind.Allowed && RuleMatchesSource(rule, sourceGroupName!, sourceProjectName))
			.ToImmutableArray();
		if (sourceAllowedRules.IsDefaultOrEmpty)
		{
			var result = new ProjectReferenceEvaluation(true, string.Empty, null, sourceGroupName, targetGroupName);

			return result;
		}

		if (string.Equals(sourceGroupName, targetGroupName, StringComparison.Ordinal))
		{
			var explicitSelfEdge = sourceAllowedRules.FirstOrDefault(rule =>
				string.Equals(rule.From, sourceGroupName, StringComparison.Ordinal)
				&& string.Equals(rule.To, targetGroupName, StringComparison.Ordinal)
				&& rule.MatchesTargetProject(targetProjectName));
			if (explicitSelfEdge.Equals(default(ProjectReferenceRule)))
			{
				var sameGroupResult = new ProjectReferenceEvaluation(false, $"same-group reference from '{sourceGroupName}' to '{targetGroupName}' requires an explicit self-edge", null, sourceGroupName, targetGroupName);

				return sameGroupResult;
			}
		}

		var allowedRule = FindMatchingRule(sourceAllowedRules, sourceGroupName!, targetGroupName!, sourceProjectName, targetProjectName, ProjectReferenceRuleKind.Allowed);
		if (allowedRule is { } allowed)
		{
			var result = new ProjectReferenceEvaluation(true, string.Empty, allowed, sourceGroupName, targetGroupName);

			return result;
		}

		var reason = sourceGroupName == targetGroupName
			? $"same-group reference from '{sourceGroupName}' to '{targetGroupName}' requires an explicit self-edge"
			: $"no AllowedProjectReference permits project group '{sourceGroupName}' to reference project group '{targetGroupName}'";
		var finalResult = new ProjectReferenceEvaluation(false, reason, null, sourceGroupName, targetGroupName);

		return finalResult;
	}

	public static ProjectGroup? MatchProjectGroup(ImmutableArray<ProjectGroup> groups, string projectName)
	{
		foreach (var group in groups)
		{
			if (group.Matchers.Any(matcher => matcher.Matches(projectName)))
			{
				return group;
			}
		}

		return null;
	}

	private static ProjectReferenceEvaluation EvaluateWithoutAllowlist(ProjectGroup? sourceGroup, ProjectGroup? targetGroup)
	{
		var sourceGroupName = sourceGroup.HasValue ? sourceGroup.Value.Name : null;
		var targetGroupName = targetGroup.HasValue ? targetGroup.Value.Name : null;
		var finalResult = new ProjectReferenceEvaluation(true, string.Empty, null, sourceGroupName, targetGroupName);

		return finalResult;
	}

	private static ProjectReferenceRule? FindMatchingRule(IEnumerable<ProjectReferenceRule> rules, string sourceGroupName, string targetGroupName, string sourceProjectName, string targetProjectName, ProjectReferenceRuleKind kind)
	{
		foreach (var rule in rules)
		{
			if (rule.Kind != kind)
			{
				continue;
			}

			if (RuleMatches(rule, sourceGroupName, targetGroupName, sourceProjectName, targetProjectName))
			{
				return rule;
			}
		}

		return null;
	}

	private static bool RuleMatches(ProjectReferenceRule rule, string sourceGroupName, string targetGroupName, string sourceProjectName, string targetProjectName)
	{
		var result = RuleMatchesSource(rule, sourceGroupName, sourceProjectName) && RuleMatchesTarget(rule, targetGroupName, targetProjectName);

		return result;
	}

	private static bool RuleMatchesSource(ProjectReferenceRule rule, string sourceGroupName, string sourceProjectName)
	{
		var result = (rule.From == "*" || string.Equals(rule.From, sourceGroupName, StringComparison.Ordinal))
			&& rule.MatchesSourceProject(sourceProjectName);

		return result;
	}

	private static bool RuleMatchesTarget(ProjectReferenceRule rule, string targetGroupName, string targetProjectName)
	{
		var result = (rule.To == "*" || string.Equals(rule.To, targetGroupName, StringComparison.Ordinal))
			&& rule.MatchesTargetProject(targetProjectName);

		return result;
	}
}
