using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

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
