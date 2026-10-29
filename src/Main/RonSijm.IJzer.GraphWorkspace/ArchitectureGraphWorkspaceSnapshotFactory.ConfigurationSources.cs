using RonSijm.IJzer.ConfigurationEditing.Document;
using RonSijm.IJzer.Core.Configuration.Document.Model;
using RonSijm.IJzer.Core.Configuration.Document.Sources;
using RonSijm.IJzer.Workspace.Analysis;

namespace RonSijm.IJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
    private static ArchitectureConfigurationSource ResolveConfigurationSource(ProjectAnalysisResult project)
    {
        if (TryResolveConfigurationSource(project, out var result))
        {
            return result;
        }

        throw new ArchitectureGraphWorkspaceException("No editable ArchitecturalLevels config source was found. Add Architecture.anl or AssemblyMetadata(\"IJzerSettings\", ...) to at least one project.");
    }

    private static bool TryResolveConfigurationSource(ProjectAnalysisResult project, out ArchitectureConfigurationSource source)
    {
        var result = ArchitectureConfigurationSourceDiscovery.TryCreateConfigurationSource(project.ConfigInputPath, project.InlineConfigSourcePath, out source);

        return result;
    }
}