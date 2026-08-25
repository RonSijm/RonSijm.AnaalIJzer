using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphEditor.Standalone.FileExtension;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using RonSijm.AnaalIJzer.GraphWorkspace;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow : Window
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
		Title = "AnaalIJzer Graph Editor";
		Width = 1280;
		Height = 860;
		logger.LogInformation("Creating main window. Initial path: {InitialPath}", initialPath);
		var root = new DockPanel();
		var menu = CreateMainMenu();
		DockPanel.SetDock(menu, Dock.Top);
		root.Children.Add(menu);
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
		var load = new Button { Content = "Load", MinWidth = 86, Margin = new Thickness(8, 0, 0, 0) };
		load.Click += (_, _) => LoadSnapshot();
		status = new TextBlock { Margin = new Thickness(8, 0, 8, 8), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
		DockPanel.SetDock(load, Dock.Right);
		DockPanel.SetDock(browse, Dock.Right);
		top.Children.Add(load);
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
}
