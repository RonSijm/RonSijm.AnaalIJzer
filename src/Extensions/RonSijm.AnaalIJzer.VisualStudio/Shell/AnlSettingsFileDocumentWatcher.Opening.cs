using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Graphs;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell;

internal sealed partial class AnlSettingsFileDocumentWatcher
{
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
