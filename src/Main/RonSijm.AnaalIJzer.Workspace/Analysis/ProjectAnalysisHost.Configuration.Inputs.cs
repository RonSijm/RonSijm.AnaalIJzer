using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;

namespace RonSijm.AnaalIJzer.Workspace;

internal sealed partial class ProjectAnalysisHost
{
	private static (string? Xml, string? Path) ReadConfigInput(ImmutableArray<AdditionalText> additionalFiles, ArchitectureConfigurationTextDocument? inlineDocument, CancellationToken cancellationToken)
	{
		var configFile = ArchitectureConfigurationDocumentLoader.FindConfigurationFile(additionalFiles);
		if (configFile is not null)
		{
			return (configFile.GetText(cancellationToken)?.ToString(), configFile.Path);
		}

		var result = inlineDocument is null
			? (null, null)
			: (inlineDocument.Content, inlineDocument.Path);

		return result;
	}

	private static AdditionalText? FindSolutionConfigFile(string solutionPath, CancellationToken cancellationToken)
	{
		var configPath = ArchitectureConfigurationSourceDiscovery.FindNearestConfigurationFilePath(solutionPath);
		if (string.IsNullOrWhiteSpace(configPath))
		{
			return null;
		}

		var result = WorkspaceAdditionalText.FromFile(configPath, cancellationToken);

		return result;
	}
}
