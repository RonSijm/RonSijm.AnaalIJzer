using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public static class SolutionTopologyEvaluator
{
    public static SolutionTopologyReferenceEvaluation Evaluate(SolutionTopologyConfig config, string sourceProjectName, string targetProjectName)
    {
        var sourceModule = MatchModule(config.Modules, sourceProjectName);
        var targetModule = MatchModule(config.Modules, targetProjectName);
        var sourceModuleName = sourceModule.HasValue ? sourceModule.Value.Name : null;
        var targetModuleName = targetModule.HasValue ? targetModule.Value.Name : null;

        if (config.RequireRecognizedProjects && sourceModule is null)
        {
            var result = new SolutionTopologyReferenceEvaluation(false, $"source project '{sourceProjectName}' is not assigned to a configured solution module", null, null, targetModuleName);

            return result;
        }

        if (config.RequireRecognizedProjects && targetModule is null)
        {
            var result = new SolutionTopologyReferenceEvaluation(false, $"target project '{targetProjectName}' is not assigned to a configured solution module", null, sourceModuleName, null);

            return result;
        }

        if (sourceModule is null || targetModule is null)
        {
            var result = new SolutionTopologyReferenceEvaluation(true, string.Empty, null, sourceModuleName, targetModuleName);

            return result;
        }

        var blockedRule = FindMatchingRule(config.Rules, sourceModuleName!, targetModuleName!, SolutionModuleReferenceRuleKind.Blocked);
        if (blockedRule is { } blocked)
        {
            var result = new SolutionTopologyReferenceEvaluation(false, $"BlockedModuleReference from '{blocked.From}' to '{blocked.To}' denies this solution edge", blocked, sourceModuleName, targetModuleName);

            return result;
        }

        var sourceAllowedRules = config.Rules
            .Where(rule => rule.Kind == SolutionModuleReferenceRuleKind.Allowed && MatchesSource(rule, sourceModuleName!))
            .ToImmutableArray();
        if (sourceAllowedRules.IsDefaultOrEmpty)
        {
            var result = new SolutionTopologyReferenceEvaluation(true, string.Empty, null, sourceModuleName, targetModuleName);

            return result;
        }

        if (string.Equals(sourceModuleName, targetModuleName, StringComparison.Ordinal)
            && FindMatchingRule(sourceAllowedRules, sourceModuleName!, targetModuleName!, SolutionModuleReferenceRuleKind.Allowed) is null)
        {
            var result = new SolutionTopologyReferenceEvaluation(false, $"same-module reference from '{sourceModuleName}' to '{targetModuleName}' requires an explicit self-edge", null, sourceModuleName, targetModuleName);

            return result;
        }

        var allowedRule = FindMatchingRule(sourceAllowedRules, sourceModuleName!, targetModuleName!, SolutionModuleReferenceRuleKind.Allowed);
        if (allowedRule is { } allowed)
        {
            var result = new SolutionTopologyReferenceEvaluation(true, string.Empty, allowed, sourceModuleName, targetModuleName);

            return result;
        }

        var reason = sourceModuleName == targetModuleName
            ? $"same-module reference from '{sourceModuleName}' to '{targetModuleName}' requires an explicit self-edge"
            : $"no AllowedModuleReference permits solution module '{sourceModuleName}' to reference solution module '{targetModuleName}'";
        var finalResult = new SolutionTopologyReferenceEvaluation(false, reason, null, sourceModuleName, targetModuleName);

        return finalResult;
    }

    public static SolutionModule? MatchModule(ImmutableArray<SolutionModule> modules, string projectName)
    {
        foreach (var module in modules)
        {
            if (!module.Matches(projectName))
            {
                continue;
            }

            return module;
        }

        return null;
    }

    private static SolutionModuleReferenceRule? FindMatchingRule(
        IEnumerable<SolutionModuleReferenceRule> rules,
        string sourceModuleName,
        string targetModuleName,
        SolutionModuleReferenceRuleKind kind)
    {
        foreach (var rule in rules)
        {
            if (rule.Kind != kind || !MatchesSource(rule, sourceModuleName) || !MatchesTarget(rule, targetModuleName))
            {
                continue;
            }

            return rule;
        }

        return null;
    }

    private static bool MatchesSource(SolutionModuleReferenceRule rule, string sourceModuleName)
    {
        var result = rule.From == "*" || string.Equals(rule.From, sourceModuleName, StringComparison.Ordinal);

        return result;
    }

    private static bool MatchesTarget(SolutionModuleReferenceRule rule, string targetModuleName)
    {
        var result = rule.To == "*" || string.Equals(rule.To, targetModuleName, StringComparison.Ordinal);

        return result;
    }
}