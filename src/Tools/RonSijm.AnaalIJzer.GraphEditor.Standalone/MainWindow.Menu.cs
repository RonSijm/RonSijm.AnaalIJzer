using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow
{
	private Menu CreateMainMenu()
	{
		var result = new Menu();
		var file = new MenuItem { Header = "_File" };
		file.Items.Add(CreateMenuItem("_Open...", (_, _) => BrowseForInput()));
		file.Items.Add(CreateMenuItem("_Load current input", (_, _) => LoadSnapshot()));
		file.Items.Add(new Separator());
		file.Items.Add(CreateMenuItem("E_xit", (_, _) => Close()));
		var tools = new MenuItem { Header = "_Tools" };
		tools.Items.Add(CreateMenuItem("_Associate .anl files", (_, _) => AssociateAnlFiles()));
		tools.Items.Add(CreateMenuItem("_Unassociate .anl files", (_, _) => UnassociateAnlFiles()));
		result.Items.Add(file);
		result.Items.Add(tools);

		return result;
	}

	private static MenuItem CreateMenuItem(string header, RoutedEventHandler clickHandler)
	{
		var result = new MenuItem { Header = header };
		result.Click += clickHandler;

		return result;
	}

	private void BrowseForInput()
	{
		var dialog = new OpenFileDialog
		{
			Title = "Open AnaalIJzer architecture settings, project, or solution",
			Filter = "AnaalIJzer settings (*.anl)|*.anl|Projects and solutions (*.csproj;*.sln;*.slnx)|*.csproj;*.sln;*.slnx|Legacy XML settings (*.xml)|*.xml|All supported inputs (*.anl;*.csproj;*.sln;*.slnx;*.xml)|*.anl;*.csproj;*.sln;*.slnx;*.xml|All files (*.*)|*.*",
			DefaultExt = ".anl",
			FilterIndex = 1,
			CheckFileExists = true,
			Multiselect = false
		};
		if (dialog.ShowDialog(this) != true)
		{
			_logger.LogDebug("Open input dialog cancelled.");
			return;
		}

		_logger.LogInformation("Open input dialog selected {Path}", dialog.FileName);
		_pathBox.Text = dialog.FileName;
		LoadSnapshot();
	}
}
