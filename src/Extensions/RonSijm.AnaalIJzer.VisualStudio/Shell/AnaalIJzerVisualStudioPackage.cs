using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Shell.Commands;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("AnaalIJzer", "Editor-only architecture awareness for AnaalIJzer rules.", "1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\AnaalIJzer.registration.json")]
[ProvideService(typeof(AnaalIJzerUnifiedSettingsProvider), IsAsyncQueryable = true)]
[ProvideOptionPage(
	typeof(AnaalIJzerOptionsPage),
	"AnaalIJzer",
	"Editor",
	0,
	0,
	true,
	UnifiedSettingsCategoryMoniker = "AnaalIJzer",
	IsInUnifiedSettings = true,
	ShouldShowUnifiedSettingsPlaceholder = false)]
[ProvideToolWindow(typeof(ArchitectureGraphToolWindow))]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideBindingPath]
[Guid(PackageIds.PackageGuidString)]
public sealed class AnaalIJzerVisualStudioPackage : AsyncPackage
{
	private AnlSettingsFileDocumentWatcher? anlSettingsFileDocumentWatcher;

	protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		ArchitectureVisualStudioLog.Initialize(this);
		ArchitectureVisualStudioLog.Info("Package initialization started.");
		try
		{
			AddService(typeof(AnaalIJzerUnifiedSettingsProvider), CreateUnifiedSettingsProviderAsync, false);
			ArchitectureVisualStudioLog.Info("Registered Unified Settings provider service.");
			var optionsPage = (AnaalIJzerOptionsPage)GetDialogPage(typeof(AnaalIJzerOptionsPage));
			ArchitectureVisualStudioLog.Info("Options page loaded.");
			ArchitectureVisualStudioOptions.Publish(optionsPage.ToEditorOptions());
			ArchitectureVisualStudioLog.Info("Published editor options.");
			await ToggleSitesDiagnosticsCommand.InitializeAsync(this);
			await ShowDependencyGraphsCommand.InitializeAsync(this);
			await ShowStatusCommand.InitializeAsync(this);
			anlSettingsFileDocumentWatcher = await AnlSettingsFileDocumentWatcher.InitializeAsync(this);
			ArchitectureVisualStudioLog.Info("Package initialization completed.");
		}
		catch (Exception exception)
		{
			ArchitectureVisualStudioLog.Exception("Package initialization failed.", exception);
			throw;
		}
	}

	protected override void Dispose(bool disposing)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (disposing)
		{
			anlSettingsFileDocumentWatcher?.Dispose();
			anlSettingsFileDocumentWatcher = null;
		}

		base.Dispose(disposing);
	}

	private Task<object?> CreateUnifiedSettingsProviderAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
	{
		var result = Task.FromResult<object?>(new AnaalIJzerUnifiedSettingsProvider(this));

		return result;
	}
}
