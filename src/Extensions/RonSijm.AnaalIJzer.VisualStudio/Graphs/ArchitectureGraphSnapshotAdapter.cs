using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Graphing;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Snapshots;
using EditingSource = RonSijm.AnaalIJzer.ConfigurationEditing.Model.ArchitectureConfigurationSource;
using EditingSourceKind = RonSijm.AnaalIJzer.ConfigurationEditing.Model.ArchitectureConfigurationSourceKind;
using EditorFocusMode = RonSijm.AnaalIJzer.Graph.ArchitectureGraphFocusMode;
using EditorSourceKind = RonSijm.AnaalIJzer.Configuration.ArchitectureConfigurationSourceKind;
using GraphFocusMode = RonSijm.AnaalIJzer.Graphing.Model.ArchitectureGraphFocusMode;

namespace RonSijm.AnaalIJzer.VisualStudio;

internal static class ArchitectureGraphSnapshotAdapter
{
	internal static ArchitectureGraphSnapshot FromEditorSnapshot(ArchitectureEditorSnapshot snapshot)
	{
		var result = FromEditorSnapshot(snapshot.GraphSnapshot);

		return result;
	}

	internal static ArchitectureGraphSnapshot FromEditorSnapshot(ArchitectureDependencyGraphSnapshot snapshot)
	{
		var layers = snapshot.Layers
			.Select(layer => new ArchitectureGraphLayer(
				layer.Path,
				layer.DisplayName,
				layer.Description,
				layer.Depth,
				layer.PaletteSlot,
				layer.IsActive,
				layer.SourcePath,
				ConvertSourceKind(layer.SourceKind),
				layer.XmlLineNumber))
			.ToImmutableArray();
		var rules = snapshot.Rules
			.Select(rule => new ArchitectureGraphRule(
				rule.From,
				rule.To,
				rule.ScopePath,
				rule.Kind,
				rule.SiteText,
				rule.AppliesToDescendants,
				rule.IsWildcard,
				rule.IsActive,
				rule.ConfiguredFrom,
				rule.ConfiguredTo,
				rule.SourcePath,
				ConvertSourceKind(rule.SourceKind),
				rule.XmlLineNumber,
				rule.XmlLinePosition,
				rule.AllowedSites,
				rule.BlockedSites,
				rule.Description))
			.ToImmutableArray();
		var result = new ArchitectureGraphSnapshot(
			snapshot.HasConfiguration,
			snapshot.HasConfigurationIssues,
			layers,
			rules,
			snapshot.ActiveLayerPaths,
			snapshot.ConfigurationIssueMessages,
			new EditingSource(
				ConvertSourceKind(snapshot.ConfigurationSource.Kind),
				snapshot.ConfigurationSource.Path),
			ConvertEvidence(snapshot.Evidence));

		return result;
	}

	private static ArchitectureGraphEvidence ConvertEvidence(ArchitectureDependencyGraphEvidence evidence)
	{
		var types = evidence.Types
			.Select(type => new ArchitectureGraphTypeEvidence(
				type.LayerPath,
				type.TypeName,
				type.FullTypeName,
				type.FilePath,
				type.LineNumber))
			.ToImmutableArray();
		var dependencies = evidence.Dependencies
			.Select(dependency => new ArchitectureGraphDependencyEvidence(
				dependency.CallerLayerPath,
				dependency.DependencyLayerPath,
				dependency.CallerTypeName,
				dependency.DependencyTypeName,
				dependency.Site,
				dependency.Status.ToString(),
				dependency.DiagnosticId,
				dependency.Reason,
				dependency.FilePath,
				dependency.LineNumber))
			.ToImmutableArray();
		var result = new ArchitectureGraphEvidence(types, dependencies);

		return result;
	}

	internal static GraphFocusMode ConvertFocusMode(EditorFocusMode focusMode)
	{
		var result = focusMode switch
		{
			EditorFocusMode.ShowAll => GraphFocusMode.ShowAll,
			EditorFocusMode.FilterToCurrent => GraphFocusMode.FilterToCurrent,
			_ => GraphFocusMode.HighlightCurrent
		};

		return result;
	}

	private static EditingSourceKind ConvertSourceKind(EditorSourceKind sourceKind)
	{
		var result = sourceKind switch
		{
			EditorSourceKind.XmlFile => EditingSourceKind.XmlFile,
			EditorSourceKind.InlineAssemblyMetadata => EditingSourceKind.InlineAssemblyMetadata,
			_ => EditingSourceKind.None
		};

		return result;
	}
}
