using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

internal static class ArchitectureVisualStudioOptions
{
	private static ArchitectureEditorOptions _current = ArchitectureEditorOptions.Default;

	internal static event EventHandler? Changed;

	internal static ArchitectureEditorOptions Current => _current;

	internal static void Publish(ArchitectureEditorOptions options)
	{
		_current = options;
		Changed?.Invoke(null, EventArgs.Empty);
	}
}
