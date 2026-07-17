using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RonSijm.AnaalIJzer.GraphEditor.Standalone.FileExtension;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf.Controls;
using RonSijm.AnaalIJzer.Tooling;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed class MainWindow : Window
{
	private readonly TextBox pathBox;
	private readonly TextBlock status;
	private readonly ArchitectureGraphEditorControl editor;
	private readonly ArchitectureGraphWorkspaceSnapshotLoader snapshotLoader = new();
	private readonly ILogger<MainWindow> logger;
	private readonly string logPath;

	public MainWindow(string? initialPath, ILoggerFactory loggerFactory, string logPath)
	{
		logger = loggerFactory.CreateLogger<MainWindow>();
		this.logPath = logPath;
		Title = "AnaalIJzer Graph Editor Harness";
		Width = 1280;
		Height = 860;
		logger.LogInformation("Creating main window. Initial path: {InitialPath}", initialPath);
		var root = new DockPanel();
		var top = new DockPanel { Margin = new Thickness(8) };
		pathBox = new TextBox { Text = initialPath ?? string.Empty, MinWidth = 540, VerticalContentAlignment = VerticalAlignment.Center };
		pathBox.KeyDown += (_, args) =>
		{
			if (args.Key == Key.Enter)
			{
				LoadSnapshot();
				args.Handled = true;
			}
		};
		var browse = new Button { Content = "Browse...", MinWidth = 86, Margin = new Thickness(8, 0, 0, 0) };
		browse.Click += (_, _) => BrowseForInput();
		var associate = new Button { Content = "Associate .anl", MinWidth = 104, Margin = new Thickness(8, 0, 0, 0) };
		associate.Click += (_, _) => AssociateAnlFiles();
		var load = new Button { Content = "Load", MinWidth = 86, Margin = new Thickness(8, 0, 0, 0) };
		load.Click += (_, _) => LoadSnapshot();
		status = new TextBlock { Margin = new Thickness(8, 0, 8, 8), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
		DockPanel.SetDock(load, Dock.Right);
		DockPanel.SetDock(associate, Dock.Right);
		DockPanel.SetDock(browse, Dock.Right);
		top.Children.Add(load);
		top.Children.Add(associate);
		top.Children.Add(browse);
		top.Children.Add(pathBox);
		DockPanel.SetDock(top, Dock.Top);
		root.Children.Add(top);
		DockPanel.SetDock(status, Dock.Top);
		root.Children.Add(status);
		editor = new ArchitectureGraphEditorControl(
			ArchitectureGraphSnapshot.Empty,
			ArchitectureGraphFocusMode.ShowAll,
			logger: loggerFactory.CreateLogger<ArchitectureGraphEditorControl>(),
			snapshotReloader: _ => LoadSnapshotFromCurrentPath(),
			infoLogger: message => status.Text = message,
			warningLogger: message => status.Text = message);
		root.Children.Add(editor);
		Content = root;
		if (!string.IsNullOrWhiteSpace(initialPath))
		{
			Loaded += (_, _) => LoadSnapshot();
		}
	}

	private void LoadSnapshot()
	{
		_ = LoadSnapshotAsync();
	}

	private async Task LoadSnapshotAsync()
	{
		try
		{
			logger.LogInformation("Loading architecture graph input from {Path}", pathBox.Text);
			status.Text = "Loading " + pathBox.Text + "...";
			Mouse.OverrideCursor = Cursors.Wait;
			var snapshot = await LoadSnapshotFromCurrentPathAsync();
			editor.UpdateSnapshot(snapshot, ArchitectureGraphFocusMode.ShowAll);
			status.Text = FormatLoadedMessage(snapshot);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to load architecture graph input from {Path}", pathBox.Text);
			editor.UpdateSnapshot(ArchitectureGraphSnapshot.Empty, ArchitectureGraphFocusMode.ShowAll);
			status.Text = exception.Message + " Log: " + logPath;
			MessageBox.Show(exception.Message, "AnaalIJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
		finally
		{
			Mouse.OverrideCursor = null;
		}
	}

	private ArchitectureGraphSnapshot LoadSnapshotFromCurrentPath()
	{
		var snapshot = LoadSnapshotFromCurrentPathAsync().GetAwaiter().GetResult();

		return snapshot;
	}

	private async Task<ArchitectureGraphSnapshot> LoadSnapshotFromCurrentPathAsync()
	{
		var snapshot = await snapshotLoader.LoadAsync(pathBox.Text);
		logger.LogInformation(
			"Loaded architecture graph input from {Path}. Layers: {LayerCount}. Rules: {RuleCount}. Evidence types: {TypeCount}. Evidence dependencies: {DependencyCount}.",
			Path.GetFullPath(pathBox.Text),
			snapshot.Layers.Length,
			snapshot.Rules.Length,
			snapshot.Evidence.Types.Length,
			snapshot.Evidence.Dependencies.Length);

		return snapshot;
	}

	private string FormatLoadedMessage(ArchitectureGraphSnapshot snapshot)
	{
		var inputPath = Path.GetFullPath(pathBox.Text);
		var message = "Loaded " + snapshot.Layers.Length + " layer(s) and " + snapshot.Rules.Length + " dependency rule(s) from " + inputPath + ".";
		if (!string.IsNullOrWhiteSpace(snapshot.ConfigurationSource.Path)
		    && !string.Equals(Path.GetFullPath(snapshot.ConfigurationSource.Path), inputPath, StringComparison.OrdinalIgnoreCase))
		{
			message += " Config: " + snapshot.ConfigurationSource.Path + ".";
		}

		if (snapshot.Evidence.HasEvidence)
		{
			message += " Evidence: " + snapshot.Evidence.Types.Length + " type(s), " + snapshot.Evidence.Dependencies.Length + " dependency observation(s).";
		}

		return message;
	}

	private void BrowseForInput()
	{
		var dialog = new OpenFileDialog
		{
			Title = "Open AnaalIJzer architecture settings, project, or solution",
			Filter = "AnaalIJzer inputs (*.anl;*.xml;*.csproj;*.sln;*.slnx)|*.anl;*.xml;*.csproj;*.sln;*.slnx|AnaalIJzer settings (*.anl)|*.anl|XML files (*.xml)|*.xml|Project files (*.csproj)|*.csproj|Solution files (*.sln;*.slnx)|*.sln;*.slnx|All files (*.*)|*.*",
			CheckFileExists = true,
			Multiselect = false
		};
		if (dialog.ShowDialog(this) != true)
		{
			logger.LogDebug("Open input dialog cancelled.");
			return;
		}

		logger.LogInformation("Open input dialog selected {Path}", dialog.FileName);
		pathBox.Text = dialog.FileName;
		LoadSnapshot();
	}

	private void AssociateAnlFiles()
	{
		try
		{
			var changed = AnaalIJzerFileAssociation.AssociateAnlFiles(logger);
			status.Text = changed
				? ".anl files are now associated with the AnaalIJzer Graph Editor."
				: ".anl files were already associated with the AnaalIJzer Graph Editor.";
			MessageBox.Show(status.Text, "AnaalIJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to associate .anl files with the AnaalIJzer Graph Editor.");
			status.Text = exception.Message + " Log: " + logPath;
			MessageBox.Show(exception.Message, "AnaalIJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}
}
