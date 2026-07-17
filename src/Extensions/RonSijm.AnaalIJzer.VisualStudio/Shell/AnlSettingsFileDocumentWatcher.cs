using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell;

internal sealed class AnlSettingsFileDocumentWatcher : IVsRunningDocTableEvents, IVsSelectionEvents, IDisposable
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

	public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		TryOpenDocumentCookie(docCookie);
		return VSConstants.S_OK;
	}

	public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		TryOpenDocumentCookie(docCookie);
		return VSConstants.S_OK;
	}

	public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (elementid == (uint)VSConstants.VSSELELEMID.SEID_DocumentFrame && varValueNew is IVsWindowFrame frame)
		{
			TryOpenDocumentFrame(frame);
		}

		return VSConstants.S_OK;
	}

	public int OnSelectionChanged(
		IVsHierarchy pHierOld,
		uint itemidOld,
		IVsMultiItemSelect pMISOld,
		ISelectionContainer pSCOld,
		IVsHierarchy pHierNew,
		uint itemidNew,
		IVsMultiItemSelect pMISNew,
		ISelectionContainer pSCNew)
	{
		return VSConstants.S_OK;
	}

	public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
	{
		return VSConstants.S_OK;
	}

	public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
	{
		return VSConstants.S_OK;
	}

	public int OnAfterSave(uint docCookie)
	{
		return VSConstants.S_OK;
	}

	public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
	{
		return VSConstants.S_OK;
	}

	public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
	{
		return VSConstants.S_OK;
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

	private void TryOpenDocumentCookie(uint docCookie)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (runningDocumentTable is null)
		{
			return;
		}

		var documentData = IntPtr.Zero;
		try
		{
			var hr = runningDocumentTable.GetDocumentInfo(
				docCookie,
				out _,
				out _,
				out _,
				out var documentPath,
				out _,
				out _,
				out documentData);
			if (ErrorHandler.Failed(hr))
			{
				return;
			}

			TryOpenPath(documentPath);
		}
		finally
		{
			if (documentData != IntPtr.Zero)
			{
				Marshal.Release(documentData);
			}
		}
	}

	private void TryOpenDocumentFrame(IVsWindowFrame frame)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out var value);
		if (ErrorHandler.Failed(hr) || value is not string documentPath)
		{
			return;
		}

		TryOpenPath(documentPath);
	}

	private void TryOpenPath(string? documentPath)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (!ArchitectureVisualStudioOptions.Current.OpenAnlFilesInGraphEditor
		    || string.IsNullOrWhiteSpace(documentPath)
		    || !string.Equals(Path.GetExtension(documentPath), ".anl", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var fullPath = Path.GetFullPath(documentPath);
		if (string.Equals(lastOpenedPath, fullPath, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		lastOpenedPath = fullPath;
		ArchitectureVisualStudioLog.Info("Opening .anl settings file in dependency graph editor: " + fullPath);
		_ = package.JoinableTaskFactory.RunAsync(async () => await ArchitectureGraphToolWindowOpener.OpenAnlFileAsync(package, fullPath));
	}
}
