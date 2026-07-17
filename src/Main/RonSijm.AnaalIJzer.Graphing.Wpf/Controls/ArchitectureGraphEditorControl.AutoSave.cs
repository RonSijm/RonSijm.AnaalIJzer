using System.Collections.Immutable;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AutoSaveOnLostFocus(TextBox textBox, Func<ArchitectureConfigurationEditResult> save, bool enabled, bool clearSelection = false, bool refreshOnLostFocus = true)
	{
		if (!enabled)
		{
			return;
		}

		var lastSavedText = textBox.Text;
		var hasDeferredRefresh = false;
		var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(850) };
		timer.Tick += (_, _) =>
		{
			timer.Stop();
			SaveText(false, false);
		};
		textBox.ToolTip = "Saved automatically after a short pause and when this field loses focus.";
		textBox.TextChanged += (_, _) =>
		{
			timer.Stop();
			timer.Start();
		};
		textBox.LostFocus += (_, _) =>
		{
			timer.Stop();
			SaveText(refreshOnLostFocus, clearSelection);
		};

		void SaveText(bool refresh, bool clearAfterSave)
		{
			if (string.Equals(textBox.Text, lastSavedText, StringComparison.Ordinal))
			{
				if (refresh && hasDeferredRefresh)
				{
					RefreshAfterAutoSave(clearAfterSave);
					hasDeferredRefresh = false;
				}

				return;
			}

			var result = save();
			if (result.Succeeded)
			{
				lastSavedText = textBox.Text;
				hasDeferredRefresh = !refresh;
			}

			HandleAutoSaveResult(result, clearAfterSave, refresh);
		}
	}

	private void AutoSaveOnCheckChanged(CheckBox checkBox, Func<ArchitectureConfigurationEditResult> save, bool enabled, bool clearSelection = false, bool refresh = true)
	{
		if (!enabled)
		{
			return;
		}

		checkBox.ToolTip = "Saved automatically when changed.";
		checkBox.Checked += (_, _) => HandleAutoSaveResult(save(), clearSelection, refresh);
		checkBox.Unchecked += (_, _) => HandleAutoSaveResult(save(), clearSelection, refresh);
	}

	private void AutoSaveOnSelectionChanged(ComboBox comboBox, Func<ArchitectureConfigurationEditResult> save, bool enabled, bool clearSelection = false, bool refresh = true)
	{
		if (!enabled)
		{
			return;
		}

		comboBox.ToolTip = "Saved automatically when changed.";
		comboBox.SelectionChanged += (_, _) => HandleAutoSaveResult(save(), clearSelection, refresh);
	}

	private void AutoSaveOnSiteChecks(ImmutableArray<CheckBox> checks, Func<ArchitectureConfigurationEditResult> save, bool enabled, bool refresh = true)
	{
		if (!enabled)
		{
			return;
		}

		foreach (var check in checks)
		{
			check.ToolTip = "Saved automatically when changed.";
			check.Checked += (_, _) => HandleAutoSaveResult(save(), refresh: refresh);
			check.Unchecked += (_, _) => HandleAutoSaveResult(save(), refresh: refresh);
		}
	}

	private void HandleAutoSaveResult(ArchitectureConfigurationEditResult result, bool clearSelection = false, bool refresh = true)
	{
		var message = result.Succeeded ? "Auto-saved. " + result.Message : "Auto-save failed. " + result.Message;
		var displayResult = result.Succeeded
			? ArchitectureConfigurationEditResult.Success(message)
			: ArchitectureConfigurationEditResult.Failure(message);
		HandleEditResult(displayResult, clearSelection, refresh);
	}

	private void HandleEditResult(ArchitectureConfigurationEditResult result, bool clearSelection = false, bool refresh = true)
	{
		if (result.Succeeded)
		{
			infoLogger?.Invoke(result.Message);
			if (refresh)
			{
				RefreshAfterAutoSave(clearSelection);
			}

			statusText.Text = result.Message;
			statusText.Foreground = theme.SuccessForeground;
			return;
		}

		statusText.Text = result.Message;
		statusText.Foreground = theme.ErrorForeground;
		warningLogger?.Invoke(result.Message);
	}

	private void RefreshAfterAutoSave(bool clearSelection)
	{
		TryReloadSnapshot();
		var nextSelection = clearSelection ? ArchitectureGraphSelection.None : RemapSelection(currentSelection);
		Render();
		RenderSelection(nextSelection);
	}

	private void TryReloadSnapshot()
	{
		if (snapshotReloader is null)
		{
			return;
		}

		try
		{
			layoutState.Save();
			var reloadedSnapshot = snapshotReloader(snapshot);
			logger?.LogInformation(
				"Reloaded architecture graph snapshot. Layers: {LayerCount}. Rules: {RuleCount}.",
				reloadedSnapshot.Layers.Length,
				reloadedSnapshot.Rules.Length);
			snapshot = reloadedSnapshot;
			EnsureLayoutState(snapshot.ConfigurationSource);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to reload architecture graph snapshot after edit.");
			var message = "Saved, but reloading the graph failed. " + exception.Message;
			statusText.Text = message;
			statusText.Foreground = theme.ErrorForeground;
			warningLogger?.Invoke(message);
		}
	}
}
