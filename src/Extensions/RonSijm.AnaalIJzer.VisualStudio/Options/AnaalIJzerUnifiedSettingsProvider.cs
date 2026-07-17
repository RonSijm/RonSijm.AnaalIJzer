using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

[Guid(ServiceGuidString)]
internal sealed class AnaalIJzerUnifiedSettingsProvider : IExternalSettingsProvider
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

	private static object? GetValue(string moniker, AnaalIJzerOptionsPage options)
	{
		var result = moniker switch
		{
			AnaalIJzerUnifiedSettingMonikers.EnableInlineLayerBadges => (object)options.EnableInlineLayerBadges,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerCodeLens => (object)options.EnableLayerCodeLens,
			AnaalIJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer => (object)options.ShowLayerBadgesWhenNotInLayer,
			AnaalIJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges => (object)options.ShowGlobalLayerRulesInBadges,
			AnaalIJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges => (object)options.ShowLinearCallChainInBadges,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerGlyphs => (object)options.EnableLayerGlyphs,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerBlockHighlight => (object)options.EnableLayerBlockHighlight,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerBackgroundTint => (object)options.EnableLayerBackgroundTint,
			AnaalIJzerUnifiedSettingMonikers.ShowAllLayerInformation => (object)options.ShowAllLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowConstructorLayerInformation => (object)options.ShowConstructorLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodLayerInformation => (object)options.ShowMethodLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation => (object)options.ShowMethodReturnLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowFieldLayerInformation => (object)options.ShowFieldLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowPropertyLayerInformation => (object)options.ShowPropertyLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowLocalLayerInformation => (object)options.ShowLocalLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowNewLayerInformation => (object)options.ShowNewLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation => (object)options.ShowGenericInvocationLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation => (object)options.ShowGenericArgumentLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation => (object)options.ShowInheritanceLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation => (object)options.ShowInterfaceImplementationLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowAttributeLayerInformation => (object)options.ShowAttributeLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation => (object)options.ShowStaticMemberLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics => (object)options.ShowAllSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics => (object)options.ShowConstructorSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics => (object)options.ShowMethodSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics => (object)options.ShowMethodReturnSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics => (object)options.ShowFieldSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics => (object)options.ShowPropertySiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics => (object)options.ShowLocalSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics => (object)options.ShowNewSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics => (object)options.ShowGenericInvocationSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics => (object)options.ShowGenericArgumentSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics => (object)options.ShowInheritanceSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics => (object)options.ShowInterfaceImplementationSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics => (object)options.ShowAttributeSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics => (object)options.ShowStaticMemberSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.DependencyGraphFocusMode => (object)options.DependencyGraphFocusMode.ToString(),
			AnaalIJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor => (object)options.OpenAnlFilesInGraphEditor,
			AnaalIJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs => (object)options.IncludeCodeEvidenceInDependencyGraphs,
			_ => null
		};

		return result;
	}

	private static bool TrySetValue<T>(string moniker, T value, AnaalIJzerOptionsPage options)
	{
		if (moniker == AnaalIJzerUnifiedSettingMonikers.DependencyGraphFocusMode)
		{
			var text = Convert.ToString(value, CultureInfo.InvariantCulture);
			var parsed = Enum.TryParse(text, ignoreCase: true, out ArchitectureGraphFocusMode focusMode);
			if (parsed)
			{
				options.DependencyGraphFocusMode = focusMode;
			}

			return parsed;
		}

		if (value is not bool booleanValue)
		{
			return false;
		}

		var result = TrySetBooleanValue(moniker, booleanValue, options);

		return result;
	}

	private static bool TrySetBooleanValue(string moniker, bool value, AnaalIJzerOptionsPage options)
	{
		var result = true;
		switch (moniker)
		{
			case AnaalIJzerUnifiedSettingMonikers.EnableInlineLayerBadges:
				options.EnableInlineLayerBadges = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerCodeLens:
				options.EnableLayerCodeLens = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer:
				options.ShowLayerBadgesWhenNotInLayer = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges:
				options.ShowGlobalLayerRulesInBadges = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges:
				options.ShowLinearCallChainInBadges = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerGlyphs:
				options.EnableLayerGlyphs = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerBlockHighlight:
				options.EnableLayerBlockHighlight = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerBackgroundTint:
				options.EnableLayerBackgroundTint = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAllLayerInformation:
				options.ShowAllLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowConstructorLayerInformation:
				options.ShowConstructorLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodLayerInformation:
				options.ShowMethodLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation:
				options.ShowMethodReturnLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowFieldLayerInformation:
				options.ShowFieldLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowPropertyLayerInformation:
				options.ShowPropertyLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLocalLayerInformation:
				options.ShowLocalLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowNewLayerInformation:
				options.ShowNewLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation:
				options.ShowGenericInvocationLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation:
				options.ShowGenericArgumentLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation:
				options.ShowInheritanceLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation:
				options.ShowInterfaceImplementationLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAttributeLayerInformation:
				options.ShowAttributeLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation:
				options.ShowStaticMemberLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics:
				options.ShowAllSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics:
				options.ShowConstructorSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics:
				options.ShowMethodSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics:
				options.ShowMethodReturnSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics:
				options.ShowFieldSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics:
				options.ShowPropertySiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics:
				options.ShowLocalSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics:
				options.ShowNewSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics:
				options.ShowGenericInvocationSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics:
				options.ShowGenericArgumentSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics:
				options.ShowInheritanceSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics:
				options.ShowInterfaceImplementationSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics:
				options.ShowAttributeSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics:
				options.ShowStaticMemberSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor:
				options.OpenAnlFilesInGraphEditor = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs:
				options.IncludeCodeEvidenceInDependencyGraphs = value;
				break;
			default:
				result = false;
				break;
		}

		return result;
	}
}
