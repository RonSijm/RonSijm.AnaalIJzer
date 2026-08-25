using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

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
