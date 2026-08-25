using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
	private const double NodeColumnWidth = 220;
	private const double NodeRowHeight = 120;
	private const double NodeStartX = 36;
	private const double NodeStartY = 44;
	private const double NodeVisualWidth = 170;
	private const double NodeVisualHeight = 72;
	private const double BoundaryPaddingX = 28;
	private const double BoundaryPaddingTop = 36;
	private const double BoundaryPaddingBottom = 24;
	private const double BlockRowGap = 48;
	private const double BlockHorizontalGap = 32;

	public static ImmutableArray<ArchitectureGraphGroupViewModel> Build(ArchitectureGraphSnapshot snapshot, ArchitectureGraphFocusMode focusMode, bool includeEvidence = false)
	{
		if (!snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			return ImmutableArray<ArchitectureGraphGroupViewModel>.Empty;
		}

		var groups = ImmutableArray.CreateBuilder<ArchitectureGraphGroupViewModel>();
		var layerGroups = BuildConcreteGroups(snapshot, focusMode, includeEvidence);
		groups.AddRange(layerGroups);
		var wildcardRules = snapshot.Rules.Where(rule => rule.IsWildcard).ToImmutableArray();
		if (wildcardRules.Length > 0)
		{
			var wildcardActive = wildcardRules.Any(rule => rule.IsActive);
			var wildcardDiagram = BuildWildcardDiagram(snapshot, wildcardRules);
			groups.Add(new ArchitectureGraphGroupViewModel(
				"Wildcard and global rules",
				wildcardActive,
				focusMode == ArchitectureGraphFocusMode.HighlightCurrent && wildcardActive,
				ImmutableArray<string>.Empty,
				wildcardRules.Select(FormatRule).ToImmutableArray(),
				wildcardDiagram.Nodes,
				wildcardDiagram.Edges,
				snapshot.ConfigurationSource));
		}

		var builtGroups = groups.ToImmutable();
		if (focusMode != ArchitectureGraphFocusMode.FilterToCurrent || !builtGroups.Any(group => group.IsActive))
		{
			return builtGroups;
		}

		var result = builtGroups.Where(group => group.IsActive).ToImmutableArray();

		return result;
	}
}
