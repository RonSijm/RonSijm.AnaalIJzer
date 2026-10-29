using System.Windows;
using Microsoft.Extensions.Logging;
using RonSijm.IJzer.GraphEditor.Standalone.FileExtension;

namespace RonSijm.IJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow
{
    private void AssociateAnlFiles()
    {
        try
        {
            var changed = IJzerFileAssociation.AssociateAnlFiles(_logger);
            _status.Text = changed
                ? ".anl files are now associated with the IJzer Graph Editor."
                : ".anl files were already associated with the IJzer Graph Editor.";
            MessageBox.Show(_status.Text, "IJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to associate .anl files with the IJzer Graph Editor.");
            _status.Text = exception.Message + " Log: " + _logPath;
            MessageBox.Show(exception.Message, "IJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UnassociateAnlFiles()
    {
        try
        {
            var changed = IJzerFileAssociation.UnassociateAnlFiles(_logger);
            _status.Text = changed
                ? ".anl files are no longer associated with the IJzer Graph Editor."
                : ".anl files were not associated with the IJzer Graph Editor.";
            MessageBox.Show(_status.Text, "IJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to unassociate .anl files from the IJzer Graph Editor.");
            _status.Text = exception.Message + " Log: " + _logPath;
            MessageBox.Show(exception.Message, "IJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}