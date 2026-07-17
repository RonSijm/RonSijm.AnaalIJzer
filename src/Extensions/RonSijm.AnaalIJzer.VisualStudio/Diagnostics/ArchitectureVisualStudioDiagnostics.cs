using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

internal static class ArchitectureVisualStudioDiagnostics
{
	private static string current = "AnaalIJzer Visual Studio companion is loaded. Open a C# document in a configured project to start editor analysis.";

	internal static string Current => current;

	internal static void Publish(string message)
	{
		current = message;
	}

	internal static string FormatSnapshot(string filePath, ArchitectureEditorSnapshot snapshot)
	{
		if (!snapshot.HasConfiguration)
		{
			return "AnaalIJzer did not find architecture settings for the active document.\r\n"
			       + $"Document: {filePath}\r\n"
			       + "Checked Roslyn analyzer AdditionalFiles, inline AssemblyMetadata(\"AnaalIJzerSettings\", ...), and the nearest Architecture.anl above the document.";
		}

		if (snapshot.HasConfigurationIssues)
		{
			var result = "AnaalIJzer found architecture settings, but they contain configuration issues.\r\n"
			             + $"Document: {filePath}\r\n"
			             + string.Join("\r\n", snapshot.ConfigurationIssueMessages.Select(message => "- " + message));

			return result;
		}

		return "AnaalIJzer is active for the current document.\r\n"
		       + $"Document: {filePath}\r\n"
		       + $"Layer indicators: {snapshot.LayerIndicators.Length}\r\n"
		       + $"Dependency site indicators: {snapshot.SiteIndicators.Length}\r\n"
		       + $"Graph layers: {snapshot.GraphSnapshot.Layers.Length}\r\n"
		       + $"Graph rules: {snapshot.GraphSnapshot.Rules.Length}";
	}
}
