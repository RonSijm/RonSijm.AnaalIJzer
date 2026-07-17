using System.Windows.Controls;
using RonSijm.AnaalIJzer.Graphing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf;
using RonSijm.AnaalIJzer.Graphing.Wpf.Controls;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio;

internal sealed class ArchitectureGraphToolWindowControl : UserControl
{
	private readonly ArchitectureGraphEditorControl editor;
	private JoinableTask? renderTask;

	public ArchitectureGraphToolWindowControl()
	{
		var root = new Grid();
		ArchitectureVisualStudioTheme.ApplyToToolWindow(root);
		ArchitectureVisualStudioTheme.ApplyBackground(root);
		editor = new ArchitectureGraphEditorControl(
			ArchitectureGraphToolWindowState.Current,
			ArchitectureGraphSnapshotAdapter.ConvertFocusMode(ArchitectureVisualStudioOptions.Current.DependencyGraphFocusMode),
			ArchitectureVisualStudioTheme.CreateEditorTheme(root),
			ArchitectureVisualStudioLog.Info,
			ArchitectureVisualStudioLog.Warning,
			snapshotReloader: ReloadSnapshot);
		root.Children.Add(editor);
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
		renderTask = null;
	}

	private void StateChanged(object? sender, EventArgs e)
	{
#pragma warning disable VSSDK007
		renderTask = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
#pragma warning restore VSSDK007
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Render();
		});
	}

	private void Render()
	{
		editor.UpdateSnapshot(
			ArchitectureGraphToolWindowState.Current,
			ArchitectureGraphSnapshotAdapter.ConvertFocusMode(ArchitectureVisualStudioOptions.Current.DependencyGraphFocusMode));
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
