using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell.Commands;

internal sealed class ToggleSitesDiagnosticsCommand
{
	private readonly AsyncPackage _package;
	private readonly OleMenuCommand _command;

	private ToggleSitesDiagnosticsCommand(AsyncPackage package, OleMenuCommandService commandService)
	{
		this._package = package;
		var commandId = new CommandID(PackageIds.CommandSet, PackageIds.ToggleSitesDiagnosticsCommandId);
		_command = new OleMenuCommand(Execute, commandId);
		_command.BeforeQueryStatus += BeforeQueryStatus;
		commandService.AddCommand(_command);
		ArchitectureVisualStudioLog.Info("Registered command: AnaalIJzer.ToggleSitesDiagnostics.");
	}

	internal static async Task InitializeAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
		{
			_ = new ToggleSitesDiagnosticsCommand(package, commandService);
			return;
		}

		ArchitectureVisualStudioLog.Warning("Could not register Toggle Sites Diagnostics command because IMenuCommandService was unavailable.");
	}

	private void BeforeQueryStatus(object sender, EventArgs e)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var optionsPage = (AnaalIJzerOptionsPage)_package.GetDialogPage(typeof(AnaalIJzerOptionsPage));
		_command.Checked = optionsPage.ToEditorOptions().EnableSitesDiagnostics;
	}

	private void Execute(object sender, EventArgs e)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var optionsPage = (AnaalIJzerOptionsPage)_package.GetDialogPage(typeof(AnaalIJzerOptionsPage));
		var options = SitesDiagnosticsCommandState.Toggle(optionsPage.ToEditorOptions());
		optionsPage.ApplyEditorOptions(options);
		optionsPage.SaveSettingsToStorage();
		ArchitectureVisualStudioOptions.Publish(options);
		ArchitectureVisualStudioLog.Info("Toggled Sites Diagnostics to " + options.EnableSitesDiagnostics + ".");
	}
}
