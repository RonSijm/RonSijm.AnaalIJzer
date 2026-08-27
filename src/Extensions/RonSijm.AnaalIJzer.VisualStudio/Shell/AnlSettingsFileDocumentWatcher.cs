using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell;

internal sealed partial class AnlSettingsFileDocumentWatcher : IVsRunningDocTableEvents, IVsSelectionEvents, IDisposable
{
	private readonly AsyncPackage _package;
	private IVsRunningDocumentTable? _runningDocumentTable;
	private IVsMonitorSelection? _monitorSelection;
	private uint _runningDocumentTableCookie;
	private uint _monitorSelectionCookie;
	private string? _lastOpenedPath;

	private AnlSettingsFileDocumentWatcher(AsyncPackage package)
	{
		this._package = package;
	}

	internal static async Task<AnlSettingsFileDocumentWatcher> InitializeAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		var watcher = new AnlSettingsFileDocumentWatcher(package);
		await watcher.InitializeCoreAsync();
		return watcher;
	}

	public void Dispose()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (_runningDocumentTable is not null && _runningDocumentTableCookie != 0)
		{
			ErrorHandler.ThrowOnFailure(_runningDocumentTable.UnadviseRunningDocTableEvents(_runningDocumentTableCookie));
			_runningDocumentTableCookie = 0;
		}

		if (_monitorSelection is not null && _monitorSelectionCookie != 0)
		{
			ErrorHandler.ThrowOnFailure(_monitorSelection.UnadviseSelectionEvents(_monitorSelectionCookie));
			_monitorSelectionCookie = 0;
		}
	}

	private async Task InitializeCoreAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

		_runningDocumentTable = await _package.GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
		if (_runningDocumentTable is not null)
		{
			ErrorHandler.ThrowOnFailure(_runningDocumentTable.AdviseRunningDocTableEvents(this, out _runningDocumentTableCookie));
			ArchitectureVisualStudioLog.Info("Registered .anl settings file running-document watcher.");
		}
		else
		{
			ArchitectureVisualStudioLog.Warning("Could not register .anl running-document watcher because IVsRunningDocumentTable was unavailable.");
		}

		_monitorSelection = await _package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
		if (_monitorSelection is not null)
		{
			ErrorHandler.ThrowOnFailure(_monitorSelection.AdviseSelectionEvents(this, out _monitorSelectionCookie));
			ArchitectureVisualStudioLog.Info("Registered .anl active-document watcher.");
		}
		else
		{
			ArchitectureVisualStudioLog.Warning("Could not register .anl active-document watcher because IVsMonitorSelection was unavailable.");
		}
	}
}
