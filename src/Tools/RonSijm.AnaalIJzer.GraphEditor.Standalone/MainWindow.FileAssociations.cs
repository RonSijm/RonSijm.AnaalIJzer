using System.Windows;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphEditor.Standalone.FileExtension;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow
{
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

	private void UnassociateAnlFiles()
	{
		try
		{
			var changed = AnaalIJzerFileAssociation.UnassociateAnlFiles(logger);
			status.Text = changed
				? ".anl files are no longer associated with the AnaalIJzer Graph Editor."
				: ".anl files were not associated with the AnaalIJzer Graph Editor.";
			MessageBox.Show(status.Text, "AnaalIJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to unassociate .anl files from the AnaalIJzer Graph Editor.");
			status.Text = exception.Message + " Log: " + logPath;
			MessageBox.Show(exception.Message, "AnaalIJzer Graph Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}
}
