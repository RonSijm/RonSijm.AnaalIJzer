using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.IJzer.Core.Configuration.Document.Model;
using RonSijm.IJzer.Core.Configuration.Document.Sources;

namespace RonSijm.IJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
    private static ArchitectureConfigurationSource FindConfigurationSource(Document document, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, CancellationToken cancellationToken)
    {
        var result = ArchitectureConfigurationSourceDiscovery.FindConfigurationSource(document.FilePath, additionalFiles, compilation, cancellationToken);

        return result;
    }

    private static ImmutableArray<ArchitectureConfigurationCreationTarget> CreateConfigurationCreationTargets(Document document)
    {
        var result = ArchitectureConfigurationSourceDiscovery.CreateConfigurationCreationTargets(document.Project.FilePath, document.Project.Solution.FilePath);

        return result;
    }
}