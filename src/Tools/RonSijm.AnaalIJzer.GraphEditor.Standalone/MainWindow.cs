using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using RonSijm.AnaalIJzer.GraphModel.Model;
using RonSijm.AnaalIJzer.GraphWorkspace;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow : Window
{
	private readonly TextBox _pathBox;
	private readonly TextBlock _status;
	private readonly ArchitectureGraphEditorControl _editor;
	private readonly ArchitectureGraphWorkspaceSnapshotLoader _snapshotLoader = new();
	private readonly ILogger<MainWindow> _logger;
	private readonly string _logPath;

	public MainWindow(string? initialPath, ILoggerFactory loggerFactory, string logPath)
	{
		_logger = loggerFactory.CreateLogger<MainWindow>();
		this._logPath = logPath;
		Title = "AnaalIJzer Graph Editor";
		Width = 1280;
		Height = 860;
		_logger.LogInformation("Creating main window. Initial path: {InitialPath}", initialPath);
		var root = new DockPanel();
		var menu = CreateMainMenu();
		DockPanel.SetDock(menu, Dock.Top);
		root.Children.Add(menu);
		var top = new DockPanel { Margin = new Thickness(8) };
		_pathBox = new TextBox { Text = initialPath ?? string.Empty, MinWidth = 540, VerticalContentAlignment = VerticalAlignment.Center };
		_pathBox.KeyDown += (_, args) =>
		{
			if (args.Key == Key.Enter)
			{
				LoadSnapshot();
				args.Handled = true;
			}
		};
		var browse = new Button { Content = "Browse...", MinWidth = 86, Margin = new Thickness(8, 0, 0, 0) };
		browse.Click += (_, _) => BrowseForInput();
		var load = new Button { Content = "Load", MinWidth = 86, Margin = new Thickness(8, 0, 0, 0) };
		load.Click += (_, _) => LoadSnapshot();
		_status = new TextBlock { Margin = new Thickness(8, 0, 8, 8), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
		DockPanel.SetDock(load, Dock.Right);
		DockPanel.SetDock(browse, Dock.Right);
		top.Children.Add(load);
		top.Children.Add(browse);
		top.Children.Add(_pathBox);
		DockPanel.SetDock(top, Dock.Top);
		root.Children.Add(top);
		DockPanel.SetDock(_status, Dock.Top);
		root.Children.Add(_status);
		_editor = new ArchitectureGraphEditorControl(
			ArchitectureGraphSnapshot.Empty,
			ArchitectureGraphFocusMode.ShowAll,
			logger: loggerFactory.CreateLogger<ArchitectureGraphEditorControl>(),
			snapshotReloader: _ => LoadSnapshotFromCurrentPath(),
			infoLogger: message => _status.Text = message,
			warningLogger: message => _status.Text = message);
		root.Children.Add(_editor);
		Content = root;
		if (!string.IsNullOrWhiteSpace(initialPath))
		{
			Loaded += (_, _) => LoadSnapshot();
		}
	}
}
