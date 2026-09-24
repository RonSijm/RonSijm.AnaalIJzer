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

        var exportSurface = CreateExportSurface();
        ArchitectureGraphImageExporter.SavePng(exportSurface, path, _theme.Background);
        _infoLogger?.Invoke("Exported dependency graph image to " + path + ".");
        _logger?.LogInformation("Exported dependency graph image to {Path}", path);
    }

    private FrameworkElement CreateExportSurface()
    {
        if (_useExportSizing)
        {
            return _contentPanel;
        }

        var exportControl = new ArchitectureGraphEditorControl(_snapshot, _focusMode, _theme, logger: _logger, useExportSizing: true);
        exportControl._showCodeEvidence.IsChecked = _showCodeEvidence.IsChecked;
        exportControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        exportControl.Arrange(new Rect(exportControl.DesiredSize));
        exportControl.UpdateLayout();
        var result = exportControl;

        return result;
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
            _warningLogger?.Invoke(exception.Message);
            _logger?.LogError(exception, "Failed to export dependency graph image to {Path}", dialog.FileName);
            MessageBox.Show(exception.Message, "AnaalIJzer Dependency Graphs", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool CanExportGraphs()
    {
        var result = _snapshot.HasConfiguration
                     && !_snapshot.HasConfigurationIssues
                     && _contentPanel.Children.Count > 0;

        return result;
    }
}