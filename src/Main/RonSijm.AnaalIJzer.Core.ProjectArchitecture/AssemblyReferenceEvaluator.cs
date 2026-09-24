using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public static class AssemblyReferenceEvaluator
{
    public static AssemblyReferenceEvaluation Evaluate(ProjectArchitectureConfig config, string sourceProjectName, string assemblyIdentity)
    {
        var sourceGroup = ProjectReferenceEvaluator.MatchProjectGroup(config.ProjectGroups, sourceProjectName);
        var sourceGroupName = sourceGroup.HasValue ? sourceGroup.Value.Name : null;
        if (config.RequireRecognizedProjects && sourceGroup is null)
        {
            var unrecognizedResult = new AssemblyReferenceEvaluation(
                false,
                $"source project '{sourceProjectName}' is not assigned to a configured ProjectGroup",
                null,
                null,
                null);

            return unrecognizedResult;
        }

        if (sourceGroup is null)
        {
            var unmatchedResult = new AssemblyReferenceEvaluation(true, string.Empty, null, null, null);

            return unmatchedResult;
        }

        foreach (var policy in config.AssemblyReferencePolicies)
        {
            if (!string.Equals(policy.ProjectGroup, sourceGroupName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var forbiddenMatcher in policy.ForbiddenMatchers)
            {
                if (!forbiddenMatcher.Matches(assemblyIdentity))
                {
                    continue;
                }

                var forbiddenResult = new AssemblyReferenceEvaluation(
                    false,
                    $"the assembly matches a Forbidden policy for project group '{sourceGroupName}'",
                    policy,
                    forbiddenMatcher,
                    sourceGroupName);

                return forbiddenResult;
            }

            if (!policy.AllowedMatchers.IsDefaultOrEmpty)
            {
                var allowedMatcher = policy.AllowedMatchers.FirstOrDefault(matcher => matcher.Matches(assemblyIdentity));
                if (allowedMatcher.Equals(default(ReferenceIdentityMatcher)))
                {
                    var allowlistResult = new AssemblyReferenceEvaluation(
                        false,
                        $"the assembly does not match the Allowed assembly list for project group '{sourceGroupName}'",
                        policy,
                        null,
                        sourceGroupName);

                    return allowlistResult;
                }
            }
        }

        var result = new AssemblyReferenceEvaluation(true, string.Empty, null, null, sourceGroupName);

        return result;
    }
}