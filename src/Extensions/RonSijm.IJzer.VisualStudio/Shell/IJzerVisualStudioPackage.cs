using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using RonSijm.IJzer.VisualStudio.Diagnostics;
using RonSijm.IJzer.VisualStudio.Graphs;
using RonSijm.IJzer.VisualStudio.Options;
using RonSijm.IJzer.VisualStudio.Shell.Commands;

namespace RonSijm.IJzer.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("IJzer", "Editor-only architecture awareness for IJzer rules.", "1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\IJzer.registration.json")]
[ProvideService(typeof(IJzerUnifiedSettingsProvider), IsAsyncQueryable = true)]
[ProvideOptionPage(
    typeof(IJzerOptionsPage),
    "IJzer",
    "Editor",
    0,
    0,
    true,
    UnifiedSettingsCategoryMoniker = "IJzer",
    IsInUnifiedSettings = true,
    ShouldShowUnifiedSettingsPlaceholder = false)]
[ProvideToolWindow(typeof(ArchitectureGraphToolWindow))]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideBindingPath]
[Guid(PackageIds.PackageGuidString)]
public sealed class IJzerVisualStudioPackage : AsyncPackage
{
    private AnlSettingsFileDocumentWatcher? _anlSettingsFileDocumentWatcher;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        ArchitectureVisualStudioLog.Initialize(this);
        ArchitectureVisualStudioLog.Info("Package initialization started.");
        try
        {
            AddService(typeof(IJzerUnifiedSettingsProvider), CreateUnifiedSettingsProviderAsync, false);
            ArchitectureVisualStudioLog.Info("Registered Unified Settings provider service.");
            var optionsPage = (IJzerOptionsPage)GetDialogPage(typeof(IJzerOptionsPage));
            ArchitectureVisualStudioLog.Info("Options page loaded.");
            ArchitectureVisualStudioOptions.Publish(optionsPage.ToEditorOptions());
            ArchitectureVisualStudioLog.Info("Published editor options.");
            await ToggleSitesDiagnosticsCommand.InitializeAsync(this);
            await ShowDependencyGraphsCommand.InitializeAsync(this);
            await ShowStatusCommand.InitializeAsync(this);
            _anlSettingsFileDocumentWatcher = await AnlSettingsFileDocumentWatcher.InitializeAsync(this);
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
            _anlSettingsFileDocumentWatcher?.Dispose();
            _anlSettingsFileDocumentWatcher = null;
        }

        base.Dispose(disposing);
    }

    private Task<object?> CreateUnifiedSettingsProviderAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
    {
        var result = Task.FromResult<object?>(new IJzerUnifiedSettingsProvider(this));

        return result;
    }
}