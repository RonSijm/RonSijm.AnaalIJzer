using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell;

internal sealed partial class AnlSettingsFileDocumentWatcher
{
	public int OnAfterFirstDocumentLock(uint docCookie, uint dwRdtLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
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
		IVsMultiItemSelect pMisOld,
		ISelectionContainer pScOld,
		IVsHierarchy pHierNew,
		uint itemidNew,
		IVsMultiItemSelect pMisNew,
		ISelectionContainer pScNew)
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

	public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRdtLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
	{
		return VSConstants.S_OK;
	}

	public int OnCmdUIContextChanged(uint dwCmdUiCookie, int fActive)
	{
		return VSConstants.S_OK;
	}
}
