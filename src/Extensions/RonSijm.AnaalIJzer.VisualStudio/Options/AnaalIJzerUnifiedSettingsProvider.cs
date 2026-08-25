using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

[Guid(ServiceGuidString)]
internal sealed partial class AnaalIJzerUnifiedSettingsProvider : IExternalSettingsProvider
{
	internal const string ServiceGuidString = "af0e6eb4-77ec-4d32-a9b1-233720e65c03";

	private readonly AsyncPackage package;
	private EventHandler<ExternalSettingsChangedEventArgs>? settingValuesChanged;

	internal AnaalIJzerUnifiedSettingsProvider(AsyncPackage package)
	{
		this.package = package;
	}

	public event EventHandler<ExternalSettingsChangedEventArgs>? SettingValuesChanged
	{
		add => settingValuesChanged += value;
		remove => settingValuesChanged -= value;
	}

	public event EventHandler<EnumSettingChoicesChangedEventArgs>? EnumSettingChoicesChanged
	{
		add { }
		remove { }
	}

	public event EventHandler<DynamicMessageTextChangedEventArgs>? DynamicMessageTextChanged
	{
		add { }
		remove { }
	}

	public event EventHandler? ErrorConditionResolved
	{
		add { }
		remove { }
	}

	public async Task<ExternalSettingOperationResult<T>> GetValueAsync<T>(string moniker, CancellationToken cancellationToken)
		where T : notnull
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		var optionsPage = GetOptionsPage();
		var value = GetValue(moniker, optionsPage);
		var result = value is null
			? await ExternalSettingOperationResult.InvalidValueResultTask<T>("Unknown AnaalIJzer setting: " + moniker)
			: await ExternalSettingOperationResult.ConvertSuccessResultTask<T>(value);

		return result;
	}

	public async Task<ExternalSettingOperationResult> SetValueAsync<T>(string moniker, T value, CancellationToken cancellationToken)
		where T : notnull
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		var optionsPage = GetOptionsPage();
		var changed = TrySetValue(moniker, value, optionsPage);
		if (changed)
		{
			optionsPage.SaveSettingsToStorage();
			ArchitectureVisualStudioOptions.Publish(optionsPage.ToEditorOptions());
			settingValuesChanged?.Invoke(this, ExternalSettingsChangedEventArgs.SomeOrAll);
			ArchitectureVisualStudioLog.Info("Unified Settings updated " + moniker + ".");
		}

		var result = await ExternalSettingOperationResult.SuccessResultTask();

		return result;
	}

	public Task<string> GetMessageTextAsync(string messageId, CancellationToken cancellationToken)
	{
		var result = Task.FromResult(string.Empty);

		return result;
	}

	public Task<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>> GetEnumChoicesAsync(string enumSettingMoniker, CancellationToken cancellationToken)
	{
		IReadOnlyList<EnumChoice> choices = new[]
		{
			new EnumChoice(nameof(ArchitectureGraphFocusMode.ShowAll), "Show all graphs"),
			new EnumChoice(nameof(ArchitectureGraphFocusMode.HighlightCurrent), "Highlight current graph"),
			new EnumChoice(nameof(ArchitectureGraphFocusMode.FilterToCurrent), "Filter to current graph")
		};
		var result = ExternalSettingOperationResult.SuccessResultTask(choices);

		return result;
	}

	public Task OpenBackingStoreAsync(CancellationToken cancellationToken)
	{
		var result = Task.CompletedTask;

		return result;
	}

	private AnaalIJzerOptionsPage GetOptionsPage()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var result = (AnaalIJzerOptionsPage)package.GetDialogPage(typeof(AnaalIJzerOptionsPage));

		return result;
	}
}
