using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.GraphModel.Building;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Workspace;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
	public static ArchitectureGraphSnapshot CreateForProject(string projectPath, ProjectAnalysisResult project, CancellationToken cancellationToken)
	{
		if (project.Config.HasLayers)
		{
			var result = CreateSnapshot([project], project, cancellationToken);

			return result;
		}

		if (TryResolveConfigurationSource(project, out var source))
		{
			var result = ArchitectureGraphXmlSnapshotLoader.Load(source);

			return result;
		}

		var creationTargets = ArchitectureConfigurationSourceDiscovery.CreateConfigurationCreationTargets(projectPath, null);
		var resultWithoutConfiguration = CreateNoConfigurationSnapshot(creationTargets);

		return resultWithoutConfiguration;
	}

	public static ArchitectureGraphSnapshot CreateForSolution(string solutionPath, SolutionAnalysisResult solution, CancellationToken cancellationToken)
	{
		var representativeProject = solution.FirstConfiguredProject;
		if (representativeProject is not null)
		{
			var result = CreateSnapshot(solution.Projects, representativeProject, cancellationToken);

			return result;
		}

		var configuredButEmptyProject = solution.Projects.FirstOrDefault(project => TryResolveConfigurationSource(project, out _));
		if (configuredButEmptyProject is not null && TryResolveConfigurationSource(configuredButEmptyProject, out var source))
		{
			var result = ArchitectureGraphXmlSnapshotLoader.Load(source);

			return result;
		}

		var creationTargets = ArchitectureConfigurationSourceDiscovery.CreateConfigurationCreationTargets(null, solutionPath);
		var resultWithoutConfiguration = CreateNoConfigurationSnapshot(creationTargets);

		return resultWithoutConfiguration;
	}

	private static ArchitectureGraphSnapshot CreateNoConfigurationSnapshot(ImmutableArray<ArchitectureConfigurationCreationTarget> creationTargets)
	{
		var result = ArchitectureGraphSnapshotFactory.CreateNoConfigurationSnapshot(creationTargets);

		return result;
	}

	private static ArchitectureGraphSnapshot CreateSnapshot(ImmutableArray<ProjectAnalysisResult> projects, ProjectAnalysisResult representativeProject, CancellationToken cancellationToken)
	{
		var source = ResolveConfigurationSource(representativeProject);
		var configSnapshot = ArchitectureGraphXmlSnapshotLoader.Load(source);
		var evidence = CreateEvidence(projects, representativeProject.Config, cancellationToken);
		var exceptionReviews = CreateExceptionReviews(projects, representativeProject.Config, cancellationToken);
		var result = ArchitectureGraphSnapshotFactory.AttachEvidence(configSnapshot, evidence, exceptionReviews);

		return result;
	}
}

