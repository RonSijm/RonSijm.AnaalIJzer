using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.GraphModel.Model;
using RonSijm.AnaalIJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
	private static void AddDependencyEvidence(ProjectAnalysisResult project, AnalyzerConfiguration config, ImmutableArray<ArchitectureGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		string? ResolveLayer(INamedTypeSymbol type)
		{
			var match = FindLayer(config, type);
			var result = match is { } layerMatch && !layerMatch.Layer.IsForbidden
				? layerMatch.Layer.Name
				: null;

			return result;
		}

		foreach (var observation in ProjectDependencyScanner.Scan(project.Compilation, ResolveLayer, cancellationToken))
		{
			var evidence = CreateDependencyEvidence(observation, config, project.ProjectDirectory);
			if (evidence is null)
			{
				continue;
			}

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
	}

	private static ArchitectureGraphDependencyEvidence? CreateDependencyEvidence(ProjectDependencyObservation observation, AnalyzerConfiguration config, string projectDirectory)
	{
		var callerMatch = FindLayer(config, observation.CallerType);
		var dependencyMatch = FindLayer(config, observation.DependencyType);
		if (callerMatch is null || dependencyMatch is null)
		{
			return null;
		}

		var status = "Allowed";
		string? diagnosticId = null;
		var reason = "allowed by configured dependency rules";
		if (dependencyMatch.Value.Layer.IsForbidden)
		{
			status = "TypePolicyViolation";
			diagnosticId = ArchitecturalDiagnosticIds.ForbiddenDependency;
			reason = dependencyMatch.Value.Layer.Comment is null
				? "the type matches a global <Forbidden> rule"
				: "the type matches a global <Forbidden> rule: " + dependencyMatch.Value.Layer.Comment;
		}
		else if (config.Engine.EvaluateTypePolicy(dependencyMatch.Value, observation.DependencyType.Name, GetNamespace(observation.DependencyType), observation.DependencyType) is { } policyViolation)
		{
			status = "TypePolicyViolation";
			diagnosticId = ArchitecturalDiagnosticIds.ForbiddenDependency;
			reason = policyViolation.Reason;
		}
		else
		{
			var edgeEvaluation = config.Graph.EvaluateDependency(callerMatch.Value, dependencyMatch.Value, observation.Site);
			if (!edgeEvaluation.IsAllowed)
			{
				status = GetDeniedStatus(callerMatch.Value.Layer.Name, dependencyMatch.Value.Layer.Name, edgeEvaluation, config);
				diagnosticId = status switch
				{
					"WrongDirection" => ArchitecturalDiagnosticIds.WrongDirectionDependency,
					"SameLayer" => ArchitecturalDiagnosticIds.SameLayerDependency,
					_ => ArchitecturalDiagnosticIds.IllegalLevelDependency
				};
				reason = status == "SameLayer" && !edgeEvaluation.IsDeniedBySiteFilter
					? $"types in the same layer ('{callerMatch.Value.Layer.Name}') may not depend on each other"
					: status == "WrongDirection" && !edgeEvaluation.IsDeniedBySiteFilter
						? $"this dependency goes the wrong direction - the reverse ('{dependencyMatch.Value.Layer.Name}' -> '{callerMatch.Value.Layer.Name}') is configured"
						: edgeEvaluation.DenialReason;
			}
		}

		var result = new ArchitectureGraphDependencyEvidence(
			callerMatch.Value.Layer.Name,
			dependencyMatch.Value.Layer.Name,
			observation.CallerType.Name,
			observation.DependencyType.Name,
			observation.Site,
			status,
			diagnosticId,
			reason,
			FormatLocationPath(observation.Location, projectDirectory),
			GetLineNumber(observation.Location));

		return result;
	}

	private static string GetDeniedStatus(string callerLayerName, string dependencyLayerName, DependencyEdgeEvaluation edgeEvaluation, AnalyzerConfiguration config)
	{
		if (callerLayerName == dependencyLayerName)
		{
			return "SameLayer";
		}

		if (edgeEvaluation.IsDeniedByBlockedEdge)
		{
			return "Blocked";
		}

		if (config.Graph.HasEdge(edgeEvaluation.ScopePath, dependencyLayerName, callerLayerName))
		{
			return "WrongDirection";
		}

		var result = edgeEvaluation.IsDeniedBySiteFilter
			? "SiteFiltered"
			: "MissingAllowedDependency";

		return result;
	}

	private static string FormatLocationPath(Location location, string projectDirectory)
	{
		var path = GetLocationPath(location);
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		try
		{
			return Path.GetRelativePath(projectDirectory, path);
		}
		catch
		{
			return path;
		}
	}
}

