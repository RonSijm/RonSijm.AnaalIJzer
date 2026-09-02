using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private const string CallerLayerNameProperty = "CallerLayerName";
	private const string DependencyLayerNameProperty = "DepLayerName";
	private const string BoundaryLayerNameProperty = "BoundaryLayerName";
	private const string ConfiguredFromProperty = "DependencyRuleConfiguredFrom";
	private const string ConfiguredToProperty = "DependencyRuleConfiguredTo";

	private void AddConfigurationFixesEditor(StackPanel panel, ArchitectureGraphSelection selection)
	{
		if (_configurationFixLoader is null)
		{
			return;
		}

		var visibleFixes = GetVisibleConfigurationFixes(selection);
		panel.Children.Add(CreateSectionTitle(GetConfigurationFixSectionTitle(selection)));
		panel.Children.Add(CreateHintTextBlock(GetConfigurationFixSectionHint(selection), new Thickness(0, 2, 0, 6)));

		var actionPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
		var refreshButton = new Button
		{
			Content = _isLoadingConfigurationFixes ? "Finding..." : GetConfigurationFixRefreshLabel(selection),
			IsEnabled = !_isLoadingConfigurationFixes && !_isApplyingConfigurationFix,
			Margin = new Thickness(0, 0, 8, 0),
			MinWidth = 120
		};
		refreshButton.Click += async (_, _) => await RefreshConfigurationFixesAsync();
		actionPanel.Children.Add(refreshButton);

		var applyButton = new Button
		{
			Content = _isApplyingConfigurationFix ? "Applying..." : "Apply selected fix",
			IsEnabled = _configurationFixApplier is not null
			            && !_isLoadingConfigurationFixes
			            && !_isApplyingConfigurationFix
			            && !string.IsNullOrWhiteSpace(_selectedConfigurationFixId),
			MinWidth = 140
		};
		applyButton.Click += async (_, _) => await ApplySelectedConfigurationFixAsync();
		actionPanel.Children.Add(applyButton);
		panel.Children.Add(actionPanel);

		if (!string.IsNullOrWhiteSpace(visibleFixes.Message))
		{
			panel.Children.Add(CreateHintTextBlock(visibleFixes.Message, new Thickness(0, 0, 0, 6)));
		}

		if (visibleFixes.Proposals.IsDefaultOrEmpty)
		{
			return;
		}

		var selectedProposal = GetSelectedConfigurationFixProposal(visibleFixes);
		var proposalBox = new ComboBox
		{
			ItemsSource = visibleFixes.Proposals,
			DisplayMemberPath = nameof(ArchitectureGraphConfigurationFixProposal.Title),
			SelectedValuePath = nameof(ArchitectureGraphConfigurationFixProposal.Id),
			SelectedValue = selectedProposal?.Id,
			Margin = new Thickness(0, 0, 0, 6)
		};
		proposalBox.SelectionChanged += (_, _) =>
		{
			_selectedConfigurationFixId = proposalBox.SelectedValue as string;
			RenderSelection(_currentSelection);
		};
		panel.Children.Add(proposalBox);

		if (selectedProposal is null)
		{
			return;
		}

		AddReadOnlyRow(panel, "Diagnostic", selectedProposal.DiagnosticId);
		AddReadOnlyRow(panel, "Risk", selectedProposal.Risk);
		AddReadOnlyRow(panel, "Target", selectedProposal.TargetPath);
		panel.Children.Add(CreateSectionTitle("Summary"));
		panel.Children.Add(CreateHintTextBlock(selectedProposal.Summary, new Thickness(0, 0, 0, 6)));
		panel.Children.Add(CreateSectionTitle("Preview"));
		panel.Children.Add(new TextBox
		{
			Text = selectedProposal.PreviewDiff,
			IsReadOnly = true,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			MinHeight = 160
		});
	}

	private ArchitectureGraphConfigurationFixProposal? GetSelectedConfigurationFixProposal(ArchitectureGraphConfigurationFixCollection visibleFixes)
	{
		var selectedProposal = visibleFixes.Proposals.FirstOrDefault(proposal => string.Equals(proposal.Id, _selectedConfigurationFixId, StringComparison.Ordinal));
		if (selectedProposal is not null)
		{
			return selectedProposal;
		}

		var result = visibleFixes.Proposals.FirstOrDefault();

		return result;
	}

	private async Task RefreshConfigurationFixesAsync()
	{
		if (_configurationFixLoader is null || _isLoadingConfigurationFixes)
		{
			return;
		}

		_isLoadingConfigurationFixes = true;
		RenderSelection(_currentSelection);

		try
		{
			var result = await _configurationFixLoader(CancellationToken.None);
			_configurationFixes = result;
			_selectedConfigurationFixId = result.Proposals.FirstOrDefault()?.Id;
			var message = string.IsNullOrWhiteSpace(result.Message)
				? "Loaded " + result.Proposals.Length + " configuration fix proposal(s)."
				: result.Message;
			_statusText.Text = message;
			_statusText.Foreground = _theme.SuccessForeground;
			_infoLogger?.Invoke(message);
		}
		catch (Exception exception)
		{
			var message = "Failed to load configuration fixes. " + exception.Message;
			_statusText.Text = message;
			_statusText.Foreground = _theme.ErrorForeground;
			_warningLogger?.Invoke(message);
			_logger?.LogError(exception, "Failed to load configuration fixes.");
		}
		finally
		{
			_isLoadingConfigurationFixes = false;
			RenderSelection(_currentSelection);
		}
	}

	private async Task ApplySelectedConfigurationFixAsync()
	{
		if (_configurationFixApplier is null || _isApplyingConfigurationFix || string.IsNullOrWhiteSpace(_selectedConfigurationFixId))
		{
			return;
		}

		_isApplyingConfigurationFix = true;
		var selectedFixId = _selectedConfigurationFixId!;
		RenderSelection(_currentSelection);

		try
		{
			var result = await _configurationFixApplier(selectedFixId, CancellationToken.None);
			TryReloadSnapshot();
			var refreshedSelection = RemapSelection(_currentSelection);
			Render();
			RenderSelection(refreshedSelection);
			_configurationFixes = await _configurationFixLoader(CancellationToken.None);
			_selectedConfigurationFixId = _configurationFixes.Proposals.FirstOrDefault()?.Id;
			_statusText.Text = result.Message;
			_statusText.Foreground = _theme.SuccessForeground;
			_infoLogger?.Invoke(result.Message);
		}
		catch (Exception exception)
		{
			var message = "Failed to apply configuration fix. " + exception.Message;
			_statusText.Text = message;
			_statusText.Foreground = _theme.ErrorForeground;
			_warningLogger?.Invoke(message);
			_logger?.LogError(exception, "Failed to apply configuration fix '{FixId}'.", selectedFixId);
		}
		finally
		{
			_isApplyingConfigurationFix = false;
			RenderSelection(_currentSelection);
		}
	}

	private ArchitectureGraphConfigurationFixCollection GetVisibleConfigurationFixes(ArchitectureGraphSelection selection)
	{
		if (_configurationFixes.Proposals.IsDefaultOrEmpty || selection.Kind == ArchitectureGraphSelectionKind.None)
		{
			return _configurationFixes;
		}

		var visibleProposals = _configurationFixes.Proposals
			.Where(proposal => ProposalMatchesSelection(selection, proposal))
			.ToImmutableArray();
		var message = visibleProposals.Length == _configurationFixes.Proposals.Length
			? _configurationFixes.Message
			: visibleProposals.Length == 0
				? "No configuration fix proposals in the current project match this selection."
				: "Showing " + visibleProposals.Length + " of " + _configurationFixes.Proposals.Length + " loaded configuration fix proposal(s) for this selection.";
		var result = new ArchitectureGraphConfigurationFixCollection(message, visibleProposals);

		return result;
	}

	private static bool ProposalMatchesSelection(ArchitectureGraphSelection selection, ArchitectureGraphConfigurationFixProposal proposal)
	{
		if (selection.Kind == ArchitectureGraphSelectionKind.Layer)
		{
			var layerPath = selection.RelatedLayerPath;
			var result = MatchesLayerScope(layerPath, GetProposalProperty(proposal, CallerLayerNameProperty))
			             || MatchesLayerScope(layerPath, GetProposalProperty(proposal, DependencyLayerNameProperty))
			             || MatchesLayerScope(layerPath, GetProposalProperty(proposal, BoundaryLayerNameProperty))
			             || MatchesLayerScope(layerPath, GetProposalProperty(proposal, ConfiguredFromProperty))
			             || MatchesLayerScope(layerPath, GetProposalProperty(proposal, ConfiguredToProperty));

			return result;
		}

		if (selection.Kind == ArchitectureGraphSelectionKind.DependencyRule || selection.Kind == ArchitectureGraphSelectionKind.CodeEvidence)
		{
			var fromLayer = selection.RelatedFromLayerPath;
			var toLayer = selection.RelatedToLayerPath;
			var result = MatchesLayerScope(fromLayer, GetProposalProperty(proposal, CallerLayerNameProperty))
			             && MatchesLayerScope(toLayer, GetProposalProperty(proposal, DependencyLayerNameProperty));
			if (result)
			{
				return true;
			}

			result = MatchesLayerScope(fromLayer, GetProposalProperty(proposal, ConfiguredFromProperty))
			         && MatchesLayerScope(toLayer, GetProposalProperty(proposal, ConfiguredToProperty));

			return result;
		}

		return true;
	}

	private static string GetProposalProperty(ArchitectureGraphConfigurationFixProposal proposal, string key)
	{
		var result = proposal.DiagnosticProperties.TryGetValue(key, out var value) ? value : string.Empty;

		return result;
	}

	private static bool MatchesLayerScope(string expectedLayer, string candidateLayer)
	{
		var normalizedExpected = NormalizeLayerPath(expectedLayer);
		var normalizedCandidate = NormalizeLayerPath(candidateLayer);
		if (string.IsNullOrWhiteSpace(normalizedExpected) || string.IsNullOrWhiteSpace(normalizedCandidate))
		{
			return false;
		}

		if (string.Equals(normalizedCandidate, "*", StringComparison.Ordinal))
		{
			return false;
		}

		var result = string.Equals(normalizedExpected, normalizedCandidate, StringComparison.Ordinal)
		             || normalizedCandidate.StartsWith(normalizedExpected + "/", StringComparison.Ordinal);

		return result;
	}

	private static string NormalizeLayerPath(string value)
	{
		var result = (value ?? string.Empty).Trim();
		while (result.StartsWith("/", StringComparison.Ordinal))
		{
			result = result.Substring(1);
		}

		return result;
	}

	private static string GetConfigurationFixSectionTitle(ArchitectureGraphSelection selection)
	{
		var result = selection.Kind switch
		{
			ArchitectureGraphSelectionKind.Layer => "Configuration fixes for this layer",
			ArchitectureGraphSelectionKind.DependencyRule => "Configuration fixes for this dependency rule",
			ArchitectureGraphSelectionKind.CodeEvidence => "Configuration fixes for this observed dependency",
			_ => "Configuration fixes"
		};

		return result;
	}

	private static string GetConfigurationFixSectionHint(ArchitectureGraphSelection selection)
	{
		var result = selection.Kind switch
		{
			ArchitectureGraphSelectionKind.Layer => "These proposals come from the same configuration-fix catalog used by AnaalIJzer code fixes and Arse. The list is filtered to the selected layer.",
			ArchitectureGraphSelectionKind.DependencyRule => "These proposals come from the same configuration-fix catalog used by AnaalIJzer code fixes and Arse. The list is filtered to the selected dependency rule.",
			ArchitectureGraphSelectionKind.CodeEvidence => "These proposals come from the same configuration-fix catalog used by AnaalIJzer code fixes and Arse. The list is filtered to the selected observed dependency.",
			_ => "These proposals come from the same configuration-fix catalog used by AnaalIJzer code fixes and Arse."
		};

		return result;
	}

	private static string GetConfigurationFixRefreshLabel(ArchitectureGraphSelection selection)
	{
		var result = selection.Kind switch
		{
			ArchitectureGraphSelectionKind.Layer => "Find fixes for this layer",
			ArchitectureGraphSelectionKind.DependencyRule => "Find fixes for this dependency",
			ArchitectureGraphSelectionKind.CodeEvidence => "Find fixes for this observed dependency",
			_ => "Find config fixes"
		};

		return result;
	}
}
