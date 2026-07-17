using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio;

internal static class ArchitectureGraphToolWindowOpener
{
	internal static async Task OpenCurrentAsync(AsyncPackage package)
	{
		await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		var snapshot = ArchitectureGraphToolWindowState.Current;
		ArchitectureVisualStudioLog.Info(
			"Opening dependency graph tool window. Current graph state: hasConfiguration="
			+ snapshot.HasConfiguration
			+ ", hasIssues="
			+ snapshot.HasConfigurationIssues
			+ ", layers="
			+ snapshot.Layers.Length
			+ ", rules="
			+ snapshot.Rules.Length
			+ ".");
		await ShowWindowAsync(package);
	}

	internal static async Task OpenAnlFileAsync(AsyncPackage package, string path)
	{
		try
		{
			await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			ArchitectureGraphToolWindowState.Publish(snapshot);
			await ShowWindowAsync(package);
		}
		catch (Exception exception)
		{
			ArchitectureVisualStudioLog.Exception("Could not open .anl settings file in dependency graph editor.", exception);
		}
	}

	private static async Task ShowWindowAsync(AsyncPackage package)
	{
		await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		var window = await package.FindToolWindowAsync(typeof(ArchitectureGraphToolWindow), 0, true, package.DisposalToken);
		if (window?.Frame is not IVsWindowFrame frame)
		{
			throw new InvalidOperationException("Visual Studio did not create an AnaalIJzer dependency graph tool window frame.");
		}

		ErrorHandler.ThrowOnFailure(frame.Show());
		ArchitectureVisualStudioLog.Info("AnaalIJzer dependency graph tool window frame shown.");
	}
}
