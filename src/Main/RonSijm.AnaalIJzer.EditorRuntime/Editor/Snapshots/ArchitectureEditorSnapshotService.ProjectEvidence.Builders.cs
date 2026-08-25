using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.ObservedDependencies;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddProjectDependencyEvidence(ProjectDependencyObservation observation, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies)
	{
		var callerMatch = config.Engine.FindLayer(observation.CallerType.Name, observation.CallerType.ContainingNamespace?.ToDisplayString() ?? string.Empty, observation.CallerType);
		var dependencyMatch = config.Engine.FindLayer(observation.DependencyType.Name, observation.DependencyType.ContainingNamespace?.ToDisplayString() ?? string.Empty, observation.DependencyType);
		if (callerMatch is null || callerMatch.Value.Layer.IsForbidden || dependencyMatch is null)
		{
			return;
		}

		var caller = new CallerInfo(observation.CallerType.Name, callerMatch.Value.Layer.Name, callerMatch.Value);
		var evidence = CreateProjectDependencyEvidence(observation, caller, dependencyMatch.Value, config);
		var key = evidence.CallerLayerPath
		          + "|"
		          + evidence.DependencyLayerPath
		          + "|"
		          + evidence.CallerTypeName
		          + "|"
		          + evidence.DependencyTypeName
		          + "|"
		          + evidence.Site
		          + "|"
		          + evidence.FilePath
		          + "|"
		          + evidence.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
		if (seenDependencies.Add(key))
		{
			dependencies.Add(evidence);
		}
	}

	private static ArchitectureGraphDependencyEvidence CreateProjectDependencyEvidence(ProjectDependencyObservation observation, CallerInfo caller, LayerMatch dependencyMatch, ProjectAnalyzerConfig config)
	{
		var decision = DependencyRuleEvaluator.Evaluate(config, caller.Match, dependencyMatch, observation.DependencyType, observation.Site);

		var location = observation.Location;
		var result = new ArchitectureGraphDependencyEvidence(
			caller.LayerPath,
			decision.DependencyLayerName,
			caller.TypeName,
			observation.DependencyType.Name,
			observation.Site,
			decision.Status.ToString(),
			decision.DiagnosticId,
			decision.Reason,
			GetLocationPath(location),
			GetLineNumber(location));

		return result;
	}

	private static string GetApiSurfaceEvidenceStatus(string diagnosticId)
	{
		var result = diagnosticId switch
		{
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage => ArchitectureDependencySiteStatus.TypePolicyViolation.ToString(),
			ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure => ArchitectureDependencySiteStatus.TypePolicyViolation.ToString(),
			_ => ArchitectureDependencySiteStatus.Allowed.ToString()
		};

		return result;
	}

	private static string GetLocationPath(Location location)
	{
		var result = location.GetSourcePath();

		return result;
	}

	private static int GetLineNumber(Location location)
	{
		var result = location.GetSourceLineNumber();

		return result;
	}
}
