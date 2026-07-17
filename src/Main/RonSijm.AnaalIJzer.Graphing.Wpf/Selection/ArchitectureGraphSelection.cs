using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Selection;

public sealed class ArchitectureGraphSelection
{
	private ArchitectureGraphSelection(
		ArchitectureGraphSelectionKind kind,
		string title,
		string subtitle,
		ArchitectureLayerEditHandle layerHandle,
		ArchitectureDependencyRuleEditHandle dependencyHandle,
		ImmutableArray<string> allowedSites,
		ImmutableArray<string> blockedSites,
		string evidenceDetails = "")
	{
		Kind = kind;
		Title = title;
		Subtitle = subtitle;
		LayerHandle = layerHandle;
		DependencyHandle = dependencyHandle;
		AllowedSites = allowedSites;
		BlockedSites = blockedSites;
		EvidenceDetails = evidenceDetails;
	}

	public ArchitectureGraphSelectionKind Kind { get; }

	public string Title { get; }

	public string Subtitle { get; }

	public ArchitectureLayerEditHandle LayerHandle { get; }

	public ArchitectureDependencyRuleEditHandle DependencyHandle { get; }

	public ImmutableArray<string> AllowedSites { get; }

	public ImmutableArray<string> BlockedSites { get; }

	public string EvidenceDetails { get; }

	public static ArchitectureGraphSelection ForLayer(ArchitectureLayerEditHandle handle)
	{
		var title = string.IsNullOrWhiteSpace(handle.LayerPath) ? "Layer" : handle.LayerPath;
		var subtitle = handle.CanEdit ? handle.SourcePath : "Read-only or unknown configuration source.";
		var result = new ArchitectureGraphSelection(
			ArchitectureGraphSelectionKind.Layer,
			title,
			subtitle,
			handle,
			ArchitectureDependencyRuleEditHandle.None,
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty);

		return result;
	}

	public static ArchitectureGraphSelection ForDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		var title = handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo;
		var subtitle = handle.CanEdit ? handle.SourcePath : "Read-only or unknown configuration source.";
		var result = new ArchitectureGraphSelection(
			ArchitectureGraphSelectionKind.DependencyRule,
			title,
			subtitle,
			ArchitectureLayerEditHandle.None,
			handle,
			handle.AllowedSites,
			handle.BlockedSites);

		return result;
	}

	public static ArchitectureGraphSelection ForCodeEvidence(string from, string to, string summary, string details)
	{
		var result = new ArchitectureGraphSelection(
			ArchitectureGraphSelectionKind.CodeEvidence,
			"Observed code dependency " + from + " -> " + to,
			summary,
			ArchitectureLayerEditHandle.None,
			ArchitectureDependencyRuleEditHandle.None,
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty,
			details);

		return result;
	}

	public static ArchitectureGraphSelection None { get; } = new(
		ArchitectureGraphSelectionKind.None,
		"Nothing selected",
		"Select a layer or dependency rule in the graph.",
		ArchitectureLayerEditHandle.None,
		ArchitectureDependencyRuleEditHandle.None,
		ImmutableArray<string>.Empty,
		ImmutableArray<string>.Empty);
}
