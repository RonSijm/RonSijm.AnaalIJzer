using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RonSijm.AnaalIJzer.Graphing.Wpf.Exporting;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	public void ExportGraphsAsPng(string path)
	{
		if (!CanExportGraphs())
		{
			throw new InvalidOperationException("There are no rendered dependency graphs to export.");
		}

		ArchitectureGraphImageExporter.SavePng(contentPanel, path, theme.Background);
		infoLogger?.Invoke("Exported dependency graph image to " + path + ".");
		logger?.LogInformation("Exported dependency graph image to {Path}", path);
	}

	private void PromptExportGraphsAsPng()
	{
		if (!CanExportGraphs())
		{
			return;
		}

		var dialog = new SaveFileDialog
		{
			Title = "Export AnaalIJzer dependency graphs",
			FileName = "architecture-dependency-graphs.png",
			DefaultExt = ".png",
			Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
			OverwritePrompt = true
		};
		if (dialog.ShowDialog() != true)
		{
			return;
		}

		try
		{
			ExportGraphsAsPng(dialog.FileName);
		}
		catch (Exception exception)
		{
			warningLogger?.Invoke(exception.Message);
			logger?.LogError(exception, "Failed to export dependency graph image to {Path}", dialog.FileName);
			MessageBox.Show(exception.Message, "AnaalIJzer Dependency Graphs", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	private bool CanExportGraphs()
	{
		var result = snapshot.HasConfiguration
		             && !snapshot.HasConfigurationIssues
		             && contentPanel.Children.Count > 0;

		return result;
	}
}
