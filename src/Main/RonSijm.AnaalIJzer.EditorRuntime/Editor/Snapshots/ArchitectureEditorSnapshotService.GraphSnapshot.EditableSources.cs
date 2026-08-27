using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static string GetEditableRulePath(DependencyEdge edge, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(edge.XmlPath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return configurationSource.Path;
		}

		return edge.XmlPath;
	}

	private static ArchitectureConfigurationSourceKind GetEditableRuleSourceKind(DependencyEdge edge, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(edge.XmlPath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return ArchitectureConfigurationSourceKind.InlineAssemblyMetadata;
		}

		return string.IsNullOrWhiteSpace(edge.XmlPath) ? ArchitectureConfigurationSourceKind.None : ArchitectureConfigurationSourceKind.XmlFile;
	}

	private static string GetEditableElementPath(string sourcePath, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(sourcePath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return configurationSource.Path;
		}

		return sourcePath;
	}

	private static ArchitectureConfigurationSourceKind GetEditableElementSourceKind(string sourcePath, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(sourcePath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return ArchitectureConfigurationSourceKind.InlineAssemblyMetadata;
		}

		return string.IsNullOrWhiteSpace(sourcePath) ? ArchitectureConfigurationSourceKind.None : ArchitectureConfigurationSourceKind.XmlFile;
	}
}
