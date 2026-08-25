using RonSijm.AnaalIJzer.BuildMetadata;

namespace RonSijm.AnaalIJzer.ProjectArchitecture;

public static class PackageReferenceEvaluator
{
	public static PackageReferenceEvaluation Evaluate(ProjectArchitectureConfig config, string sourceProjectName, string packageId, string packageVersion, PackageReferenceKind referenceKind)
	{
		var sourceGroup = ProjectReferenceEvaluator.MatchProjectGroup(config.ProjectGroups, sourceProjectName);
		var sourceGroupName = sourceGroup.HasValue ? sourceGroup.Value.Name : null;
		if (config.RequireRecognizedProjects && sourceGroup is null)
		{
			var unrecognizedResult = new PackageReferenceEvaluation(
				false,
				$"source project '{sourceProjectName}' is not assigned to a configured ProjectGroup",
				null,
				null,
				null);

			return unrecognizedResult;
		}

		if (sourceGroup is null)
		{
			var unmatchedResult = new PackageReferenceEvaluation(true, string.Empty, null, null, null);

			return unmatchedResult;
		}

		foreach (var policy in config.PackagePolicies)
		{
			if (!string.Equals(policy.ProjectGroup, sourceGroupName, StringComparison.Ordinal))
			{
				continue;
			}

			if (referenceKind == PackageReferenceKind.Transitive && !policy.IncludeTransitive)
			{
				continue;
			}

			foreach (var forbiddenMatcher in policy.ForbiddenMatchers)
			{
				if (!forbiddenMatcher.Matches(packageId))
				{
					continue;
				}

				var forbiddenResult = new PackageReferenceEvaluation(
					false,
					$"the package matches a Forbidden policy for project group '{sourceGroupName}'",
					policy,
					forbiddenMatcher,
					sourceGroupName);

				return forbiddenResult;
			}

			if (!policy.AllowedMatchers.IsDefaultOrEmpty)
			{
				var allowedMatcher = policy.AllowedMatchers.FirstOrDefault(matcher => matcher.Matches(packageId));
				if (allowedMatcher.Equals(default(PackageMatcher)))
				{
					var allowlistResult = new PackageReferenceEvaluation(
						false,
						$"the package does not match the Allowed package list for project group '{sourceGroupName}'",
						policy,
						null,
						sourceGroupName);

					return allowlistResult;
				}
			}
		}

		var result = new PackageReferenceEvaluation(true, string.Empty, null, null, sourceGroupName);

		return result;
	}
}
