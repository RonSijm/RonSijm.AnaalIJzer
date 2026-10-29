using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using RonSijm.IJzer.GraphEditor.Wpf.Controls;
using RonSijm.IJzer.GraphModel.Loading;
using RonSijm.IJzer.GraphModel.Model;
using RonSijm.IJzer.VisualStudio.Diagnostics;
using RonSijm.IJzer.VisualStudio.Graphs.ConfigurationFixes;
using RonSijm.IJzer.VisualStudio.Options;
using RonSijm.IJzer.VisualStudio.Styling;

namespace RonSijm.IJzer.VisualStudio.Graphs;

internal sealed class ArchitectureGraphToolWindowControl : UserControl
{
    private readonly ArchitectureGraphEditorControl _editor;
    private JoinableTask? _renderTask;

    public ArchitectureGraphToolWindowControl()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var configurationFixService = ArchitectureGraphConfigurationFixService.Create();
        var root = new Grid();
        ArchitectureVisualStudioTheme.ApplyToToolWindow(root);
        ArchitectureVisualStudioTheme.ApplyBackground(root);
        _editor = new ArchitectureGraphEditorControl(
            ArchitectureGraphToolWindowState.Current,
            ArchitectureVisualStudioOptions.Current.DependencyGraphFocusMode,
            ArchitectureVisualStudioTheme.CreateEditorTheme(root),
            ArchitectureVisualStudioLog.Info,
            ArchitectureVisualStudioLog.Warning,
            snapshotReloader: ReloadSnapshot,
            snapshotPublisher: ArchitectureGraphToolWindowState.Publish,
            configurationFixLoader: cancellationToken => configurationFixService.LoadAsync(ArchitectureGraphToolWindowState.CurrentContext, cancellationToken),
            configurationFixApplier: (fixId, cancellationToken) => configurationFixService.ApplyAsync(ArchitectureGraphToolWindowState.CurrentContext, fixId, cancellationToken));
        root.Children.Add(_editor);
        Content = root;
        Loaded += (_, _) => Subscribe();
        Unloaded += (_, _) => Unsubscribe();
    }

    private void Subscribe()
    {
        ArchitectureGraphToolWindowState.Changed += StateChanged;
        ArchitectureVisualStudioOptions.Changed += StateChanged;
        Render();
    }

    private void Unsubscribe()
    {
        ArchitectureGraphToolWindowState.Changed -= StateChanged;
        ArchitectureVisualStudioOptions.Changed -= StateChanged;
        _renderTask = null;
    }

    private void StateChanged(object? sender, EventArgs e)
    {
#pragma warning disable VSSDK007
        _renderTask = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
#pragma warning restore VSSDK007
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Render();
        });
    }

    private void Render()
    {
        _editor.UpdateSnapshot(
            ArchitectureGraphToolWindowState.Current,
            ArchitectureVisualStudioOptions.Current.DependencyGraphFocusMode);
    }

    private static ArchitectureGraphSnapshot ReloadSnapshot(ArchitectureGraphSnapshot snapshot)
    {
        if (!snapshot.ConfigurationSource.CanEdit)
        {
            return ArchitectureGraphToolWindowState.Current;
        }

        var reloaded = ArchitectureGraphXmlSnapshotLoader.Load(snapshot.ConfigurationSource);
        var result = new ArchitectureGraphSnapshot(
            reloaded.HasConfiguration,
            reloaded.HasConfigurationIssues,
            reloaded.Layers,
            reloaded.Rules,
            snapshot.ActiveLayerPaths,
            reloaded.ConfigurationIssueMessages,
            reloaded.ConfigurationSource,
            snapshot.Evidence);
        ArchitectureGraphToolWindowState.Publish(result);

        return result;
    }
}