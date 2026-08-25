using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphWorkspace;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow
{
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
}
