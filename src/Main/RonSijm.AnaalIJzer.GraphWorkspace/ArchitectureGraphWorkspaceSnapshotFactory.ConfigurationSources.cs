using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
	private static ArchitectureConfigurationSource ResolveConfigurationSource(ProjectAnalysisResult project)
	{
		if (TryResolveConfigurationSource(project, out var result))
		{
			return result;
		}

		throw new ArchitectureGraphWorkspaceException("No editable ArchitecturalLevels config source was found. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...) to at least one project.");
	}

	private static bool TryResolveConfigurationSource(ProjectAnalysisResult project, out ArchitectureConfigurationSource source)
	{
		var result = ArchitectureConfigurationSourceDiscovery.TryCreateConfigurationSource(project.ConfigInputPath, project.InlineConfigSourcePath, out source);

		return result;
	}
}

