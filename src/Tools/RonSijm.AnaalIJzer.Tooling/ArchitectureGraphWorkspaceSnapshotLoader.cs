using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.Graphing.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

public sealed class ArchitectureGraphWorkspaceSnapshotLoader(string configuration = "Release")
{
	private readonly string configuration = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration;

	public async Task<ArchitectureGraphSnapshot> LoadAsync(string path, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ToolingException("Choose an AnaalIJzer settings, project, or solution file first.");
		}

		var fullPath = Path.GetFullPath(path);
		var extension = Path.GetExtension(fullPath);
		if (IsSolutionExtension(extension))
		{
			var result = await AnalyzeSolutionAsync(fullPath, cancellationToken);
			var snapshot = CreateSnapshot(result.Projects, EnsureSolutionHasLayers(result), cancellationToken);

			return snapshot;
		}

		if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
		{
			var result = await AnalyzeProjectAsync(fullPath, cancellationToken);
			EnsureProjectHasLayers(result);
			var snapshot = CreateSnapshot([result], result, cancellationToken);

			return snapshot;
		}

		return ArchitectureGraphXmlSnapshotLoader.Load(fullPath);
	}

	public async Task<ArchitectureGraphSnapshot> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
	{
		var result = await AnalyzeSolutionAsync(Path.GetFullPath(solutionPath), cancellationToken);
		var snapshot = CreateSnapshot(result.Projects, EnsureSolutionHasLayers(result), cancellationToken);

		return snapshot;
	}

	private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		if (!File.Exists(projectPath))
		{
			throw new ToolingException($"Project file not found: {projectPath}");
		}

		using var host = new ProjectAnalysisHost(configuration);
		var result = await host.AnalyzeAsync(projectPath, cancellationToken);
		EnsureWorkspaceLoaded(result.WorkspaceFailures, "project");
		EnsureCompilerErrorsAbsent(result.CompilerErrors, "Project");

		return result;
	}

	private async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
	{
		if (!File.Exists(solutionPath))
		{
			throw new ToolingException($"Solution file not found: {solutionPath}");
		}

		using var host = new ProjectAnalysisHost(configuration);
		var result = await host.AnalyzeSolutionAsync(solutionPath, cancellationToken);
		if (result.Projects.Length == 0)
		{
			throw new ToolingException($"No C# projects were found in solution: {solutionPath}");
		}

		EnsureWorkspaceLoaded(result.WorkspaceFailures, "solution");
		EnsureCompilerErrorsAbsent(result.CompilerErrors, "Solution");

		return result;
	}

	private static ArchitectureGraphSnapshot CreateSnapshot(ImmutableArray<ProjectAnalysisResult> projects, ProjectAnalysisResult representativeProject, CancellationToken cancellationToken)
	{
		var source = ResolveConfigurationSource(representativeProject);
		var configSnapshot = ArchitectureGraphXmlSnapshotLoader.Load(source);
		var evidence = CreateEvidence(projects, representativeProject.Config, cancellationToken);
		var snapshot = new ArchitectureGraphSnapshot(
			configSnapshot.HasConfiguration,
			configSnapshot.HasConfigurationIssues,
			configSnapshot.Layers,
			configSnapshot.Rules,
			configSnapshot.ActiveLayerPaths,
			configSnapshot.ConfigurationIssueMessages,
			configSnapshot.ConfigurationSource,
			evidence);

		return snapshot;
	}

	private static ArchitectureConfigurationSource ResolveConfigurationSource(ProjectAnalysisResult project)
	{
		if (!string.IsNullOrWhiteSpace(project.ConfigInputPath) && File.Exists(project.ConfigInputPath))
		{
			return new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, project.ConfigInputPath);
		}

		if (!string.IsNullOrWhiteSpace(project.InlineConfigSourcePath) && File.Exists(project.InlineConfigSourcePath))
		{
			return new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, project.InlineConfigSourcePath);
		}

		throw new ToolingException("No editable ArchitecturalLevels config source was found. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...) to at least one project.");
	}

	private static ArchitectureGraphEvidence CreateEvidence(ImmutableArray<ProjectAnalysisResult> projects, AnalyzerConfiguration config, CancellationToken cancellationToken)
	{
		var types = ImmutableArray.CreateBuilder<ArchitectureGraphTypeEvidence>();
		var dependencies = ImmutableArray.CreateBuilder<ArchitectureGraphDependencyEvidence>();
		var seenTypes = new HashSet<string>(StringComparer.Ordinal);
		var seenDependencies = new HashSet<string>(StringComparer.Ordinal);
		foreach (var project in projects)
		{
			AddTypeEvidence(project, config, types, seenTypes, cancellationToken);
			AddDependencyEvidence(project, config, dependencies, seenDependencies, cancellationToken);
		}

		var result = new ArchitectureGraphEvidence(types.ToImmutable(), dependencies.ToImmutable());

		return result;
	}

	private static void AddTypeEvidence(ProjectAnalysisResult project, AnalyzerConfiguration config, ImmutableArray<ArchitectureGraphTypeEvidence>.Builder types, HashSet<string> seenTypes, CancellationToken cancellationToken)
	{
		foreach (var type in ApplicationConfigurationGenerator.GetProjectTypes(project.Compilation, cancellationToken))
		{
			var match = FindLayer(config, type);
			if (match is null || match.Value.Layer.IsForbidden)
			{
				continue;
			}

			var location = type.Locations.FirstOrDefault(location => location.IsInSource);
			var filePath = GetLocationPath(location);
			var lineNumber = GetLineNumber(location);
			var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
			var key = match.Value.Layer.Name + "|" + fullTypeName + "|" + filePath + "|" + lineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if (!seenTypes.Add(key))
			{
				continue;
			}

			types.Add(new ArchitectureGraphTypeEvidence(
				match.Value.Layer.Name,
				type.Name,
				fullTypeName,
				filePath,
				lineNumber));
		}
	}

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
		else if (config.EvaluateTypePolicy(dependencyMatch.Value, observation.DependencyType.Name, GetNamespace(observation.DependencyType), observation.DependencyType) is { } policyViolation)
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

	private static LayerMatch? FindLayer(AnalyzerConfiguration config, INamedTypeSymbol type)
	{
		var result = config.FindLayer(type.Name, GetNamespace(type), type);

		return result;
	}

	private static string GetNamespace(INamedTypeSymbol type)
	{
		var result = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();

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

	private static string GetLocationPath(Location? location)
	{
		if (location is null || !location.IsInSource)
		{
			return string.Empty;
		}

		var lineSpan = location.GetLineSpan();
		var result = string.IsNullOrWhiteSpace(lineSpan.Path)
			? location.SourceTree?.FilePath ?? string.Empty
			: lineSpan.Path;

		return result;
	}

	private static int GetLineNumber(Location? location)
	{
		if (location is null || !location.IsInSource)
		{
			return 0;
		}

		var lineSpan = location.GetLineSpan();
		var result = lineSpan.StartLinePosition.Line + 1;

		return result;
	}

	private static bool IsSolutionExtension(string extension)
	{
		var result = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	private static void EnsureWorkspaceLoaded(ImmutableArray<string> workspaceFailures, string inputKind)
	{
		if (workspaceFailures.Length > 0)
		{
			throw new ToolingException("Workspace failed to load the " + inputKind + ":" + Environment.NewLine + string.Join(Environment.NewLine, workspaceFailures));
		}
	}

	private static void EnsureCompilerErrorsAbsent(ImmutableArray<string> compilerErrors, string label)
	{
		if (compilerErrors.Length > 0)
		{
			throw new ToolingException(label + " has compiler errors:" + Environment.NewLine + string.Join(Environment.NewLine, compilerErrors));
		}
	}

	private static void EnsureProjectHasLayers(ProjectAnalysisResult result)
	{
		if (!result.Config.HasLayers)
		{
			throw new ToolingException("No ArchitecturalLevels config was found. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...).");
		}
	}

	private static ProjectAnalysisResult EnsureSolutionHasLayers(SolutionAnalysisResult result)
	{
		var representativeProject = result.FirstConfiguredProject;
		if (representativeProject is null)
		{
			throw new ToolingException("No ArchitecturalLevels config was found in the solution. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...) to at least one project.");
		}

		return representativeProject;
	}
}