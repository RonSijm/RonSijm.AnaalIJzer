using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell.Commands;

internal sealed class ShowStatusCommand
{
	private readonly AsyncPackage package;

	private ShowStatusCommand(AsyncPackage package, OleMenuCommandService commandService)
	{
		this.package = package;
		var commandId = new CommandID(PackageIds.CommandSet, PackageIds.ShowStatusCommandId);
		commandService.AddCommand(new OleMenuCommand(Execute, commandId));
		ArchitectureVisualStudioLog.Info("Registered command: AnaalIJzer.ShowStatus.");
	}

	internal static async Task InitializeAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
		{
			_ = new ShowStatusCommand(package, commandService);
			return;
		}

		ArchitectureVisualStudioLog.Warning("Could not register Show Status command because IMenuCommandService was unavailable.");
	}

	private void Execute(object sender, EventArgs e)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		_ = package.JoinableTaskFactory.RunAsync(async () =>
		{
			try
			{
				ArchitectureVisualStudioLog.Info("Show Status command executed.");
				var message = await CreateStatusMessageAsync();
				ArchitectureVisualStudioLog.Info("Show Status result: " + message.Replace(Environment.NewLine, " | "));
				await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
				VsShellUtilities.ShowMessageBox(
					package,
					message,
					"AnaalIJzer Status",
					OLEMSGICON.OLEMSGICON_INFO,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
			catch (Exception exception)
			{
				ArchitectureVisualStudioLog.Exception("Show Status command failed.", exception);
			}
		});
	}

	private async Task<string> CreateStatusMessageAsync()
	{
		await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
		var textManager = await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
		if (textManager is null)
		{
			return ArchitectureVisualStudioDiagnostics.Current + "\r\n\r\nVisual Studio did not provide IVsTextManager.";
		}

		var activeViewResult = textManager.GetActiveView(1, null, out var viewAdapter);
		if (ErrorHandler.Failed(activeViewResult) || viewAdapter is not IVsUserData userData)
		{
			return ArchitectureVisualStudioDiagnostics.Current + "\r\n\r\nNo active editor view is available.";
		}

		var wpfTextViewHostGuid = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
		userData.GetData(ref wpfTextViewHostGuid, out var host);
		if (host is not IWpfTextViewHost viewHost)
		{
			return ArchitectureVisualStudioDiagnostics.Current + "\r\n\r\nThe active editor view is not a WPF text view.";
		}

		if (await package.GetServiceAsync(typeof(SComponentModel)) is not IComponentModel componentModel)
		{
			return ArchitectureVisualStudioDiagnostics.Current + "\r\n\r\nVisual Studio did not provide the MEF component model.";
		}

		var snapshotProvider = componentModel.GetService<ArchitectureSnapshotProvider>();
		await snapshotProvider.CreateSnapshotAsync(viewHost.TextView.TextBuffer, package.DisposalToken);
		var result = ArchitectureVisualStudioDiagnostics.Current;

		return result;
	}
}
