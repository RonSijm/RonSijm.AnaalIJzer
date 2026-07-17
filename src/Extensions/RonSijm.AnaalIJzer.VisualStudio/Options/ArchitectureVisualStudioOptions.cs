using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

internal static class ArchitectureVisualStudioOptions
{
	private static ArchitectureEditorOptions current = ArchitectureEditorOptions.Default;

	internal static event EventHandler? Changed;

	internal static ArchitectureEditorOptions Current => current;

	internal static void Publish(ArchitectureEditorOptions options)
	{
		current = options;
		Changed?.Invoke(null, EventArgs.Empty);
	}
}
