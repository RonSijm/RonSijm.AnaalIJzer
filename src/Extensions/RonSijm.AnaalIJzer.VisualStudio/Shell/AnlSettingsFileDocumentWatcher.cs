using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell;

internal sealed partial class AnlSettingsFileDocumentWatcher : IVsRunningDocTableEvents, IVsSelectionEvents, IDisposable
{
	private readonly AsyncPackage package;
	private IVsRunningDocumentTable? runningDocumentTable;
	private IVsMonitorSelection? monitorSelection;
	private uint runningDocumentTableCookie;
	private uint monitorSelectionCookie;
	private string? lastOpenedPath;

	private AnlSettingsFileDocumentWatcher(AsyncPackage package)
	{
		this.package = package;
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

		if (runningDocumentTable is not null && runningDocumentTableCookie != 0)
		{
			ErrorHandler.ThrowOnFailure(runningDocumentTable.UnadviseRunningDocTableEvents(runningDocumentTableCookie));
			runningDocumentTableCookie = 0;
		}

		if (monitorSelection is not null && monitorSelectionCookie != 0)
		{
			ErrorHandler.ThrowOnFailure(monitorSelection.UnadviseSelectionEvents(monitorSelectionCookie));
			monitorSelectionCookie = 0;
		}
	}

	private async Task InitializeCoreAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		runningDocumentTable = await package.GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
		if (runningDocumentTable is not null)
		{
			ErrorHandler.ThrowOnFailure(runningDocumentTable.AdviseRunningDocTableEvents(this, out runningDocumentTableCookie));
			ArchitectureVisualStudioLog.Info("Registered .anl settings file running-document watcher.");
		}
		else
		{
			ArchitectureVisualStudioLog.Warning("Could not register .anl running-document watcher because IVsRunningDocumentTable was unavailable.");
		}

		monitorSelection = await package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
		if (monitorSelection is not null)
		{
			ErrorHandler.ThrowOnFailure(monitorSelection.AdviseSelectionEvents(this, out monitorSelectionCookie));
			ArchitectureVisualStudioLog.Info("Registered .anl active-document watcher.");
		}
		else
		{
			ArchitectureVisualStudioLog.Warning("Could not register .anl active-document watcher because IVsMonitorSelection was unavailable.");
		}
	}
}
