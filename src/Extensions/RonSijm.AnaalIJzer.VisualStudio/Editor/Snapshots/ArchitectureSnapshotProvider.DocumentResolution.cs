using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;

internal sealed partial class ArchitectureSnapshotProvider
{
	internal static async Task<ImmutableArray<AdditionalText>> ResolveAdditionalFilesAsync(Document document, string? documentPath, CancellationToken cancellationToken)
	{
		var additionalFiles = document.Project.AnalyzerOptions.AdditionalFiles;
		if (documentPath is null)
		{
			return additionalFiles;
		}

		if (ArchitectureConfigurationDocumentLoader.FindConfigurationFile(additionalFiles) is not null)
		{
			ArchitectureVisualStudioLog.Info("Project already provides an Architecture.anl additional file.");
			return additionalFiles;
		}

		var compilation = await document.Project.GetCompilationAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(ArchitectureConfigurationDocumentLoader.TryReadInlineConfigurationXml(compilation)))
		{
			ArchitectureVisualStudioLog.Info("Project already provides inline AnaalIJzer settings; skipping nearest-config fallback.");
			return additionalFiles;
		}

		var discoveredPath = ArchitectureConfigurationSourceDiscovery.FindNearestConfigurationFilePath(documentPath);
		if (discoveredPath is null || additionalFiles.Any(file => string.Equals(file.Path, discoveredPath, StringComparison.OrdinalIgnoreCase)))
		{
			ArchitectureVisualStudioLog.Info(discoveredPath is null
				? "No nearest architecture config fallback found for '" + documentPath + "'."
				: "Nearest architecture config fallback already present: '" + discoveredPath + "'.");
			return additionalFiles;
		}

		ArchitectureVisualStudioLog.Info("Adding nearest architecture config fallback: '" + discoveredPath + "'.");
		var result = additionalFiles.Add(new PhysicalAdditionalText(discoveredPath));

		return result;
	}

	private DocumentId? FindDocumentId(string filePath)
	{
		var result = workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
		if (result is not null)
		{
			return result;
		}

		result = workspace.CurrentSolution.Projects
			.SelectMany(project => project.Documents)
			.Where(document => string.Equals(document.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
			.Select(document => document.Id)
			.FirstOrDefault();

		return result;
	}
}
