using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell.Commands;

internal sealed class ShowDependencyGraphsCommand
{
	private readonly AsyncPackage package;

	private ShowDependencyGraphsCommand(AsyncPackage package, OleMenuCommandService commandService)
	{
		this.package = package;
		var commandId = new CommandID(PackageIds.CommandSet, PackageIds.ShowDependencyGraphsCommandId);
		commandService.AddCommand(new OleMenuCommand(Execute, commandId));
		ArchitectureVisualStudioLog.Info("Registered command: AnaalIJzer.ShowDependencyGraphs.");
	}

	internal static async Task InitializeAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
		{
			_ = new ShowDependencyGraphsCommand(package, commandService);
			return;
		}

		ArchitectureVisualStudioLog.Warning("Could not register Show Dependency Graphs command because IMenuCommandService was unavailable.");
	}

	private void Execute(object sender, EventArgs e)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		_ = package.JoinableTaskFactory.RunAsync(ShowDependencyGraphsAsync);
	}

	private async Task ShowDependencyGraphsAsync()
	{
		try
		{
			await ArchitectureGraphToolWindowOpener.OpenCurrentAsync(package);
		}
		catch (Exception exception)
		{
			ArchitectureVisualStudioLog.Exception("Show Dependency Graphs command failed.", exception);
			await ShowFailureAsync(exception);
		}
	}

	private async Task ShowFailureAsync(Exception exception)
	{
		await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
		VsShellUtilities.ShowMessageBox(
			package,
			"The AnaalIJzer dependency graph window could not be opened.\r\n\r\n" + exception.Message,
			"AnaalIJzer Dependency Graphs",
			OLEMSGICON.OLEMSGICON_CRITICAL,
			OLEMSGBUTTON.OLEMSGBUTTON_OK,
			OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
	}
}
