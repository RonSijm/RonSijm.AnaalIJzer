using System.Collections.Immutable;
using RonSijm.AnaalIJzer.BuildMetadata;

namespace RonSijm.AnaalIJzer.ProjectArchitecture;

public static class ProjectArchitectureAnalysisService
{
	public static ProjectArchitectureAnalysisResult Analyze(ProjectArchitectureConfig config, ArchitectureReferenceManifest manifest)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var projectReferenceViolations = ImmutableArray.CreateBuilder<ProjectReferenceViolationFinding>();
		var packageReferenceViolations = ImmutableArray.CreateBuilder<PackageReferenceViolationFinding>();

		foreach (var projectReference in manifest.ProjectReferences)
		{
			var sourceProjectName = Path.GetFileNameWithoutExtension(projectReference.SourceProjectPath);
			var targetProjectName = Path.GetFileNameWithoutExtension(projectReference.TargetProjectPath);
			var evaluation = ProjectReferenceEvaluator.Evaluate(config, sourceProjectName, targetProjectName);
			if (evaluation.IsAllowed)
			{
				continue;
			}

			var key = projectReference.SourceProjectPath + "|" + projectReference.TargetProjectPath + "|" + evaluation.ViolationReason;
			if (!seen.Add(key))
			{
				continue;
			}

			projectReferenceViolations.Add(new ProjectReferenceViolationFinding(
				projectReference.SourceProjectPath,
				sourceProjectName,
				evaluation.SourceProjectGroup,
				projectReference.TargetProjectPath,
				targetProjectName,
				evaluation.TargetProjectGroup,
				evaluation.ViolationReason,
				evaluation.MatchedRule));
		}

		foreach (var packageReference in manifest.PackageReferences)
		{
			var sourceProjectName = Path.GetFileNameWithoutExtension(packageReference.SourceProjectPath);
			var evaluation = PackageReferenceEvaluator.Evaluate(
				config,
				sourceProjectName,
				packageReference.PackageId,
				packageReference.PackageVersion,
				packageReference.ReferenceKind);
			if (evaluation.IsAllowed)
			{
				continue;
			}

			var key = packageReference.SourceProjectPath + "|" + packageReference.PackageId + "|" + packageReference.PackageVersion + "|" + packageReference.ReferenceKind + "|" + evaluation.ViolationReason;
			if (!seen.Add(key))
			{
				continue;
			}

			packageReferenceViolations.Add(new PackageReferenceViolationFinding(
				packageReference.SourceProjectPath,
				sourceProjectName,
				evaluation.SourceProjectGroup,
				packageReference.PackageId,
				packageReference.PackageVersion,
				packageReference.ReferenceKind,
				evaluation.ViolationReason,
				evaluation.MatchedPolicy,
				evaluation.MatchedMatcher));
		}

		var result = new ProjectArchitectureAnalysisResult(projectReferenceViolations.ToImmutable(), packageReferenceViolations.ToImmutable());

		return result;
	}
}
