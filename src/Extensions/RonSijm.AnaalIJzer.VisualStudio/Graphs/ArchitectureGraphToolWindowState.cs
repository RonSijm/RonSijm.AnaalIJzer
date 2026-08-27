using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.VisualStudio.Graphs;

internal static class ArchitectureGraphToolWindowState
{
	private static ArchitectureGraphSnapshot _current = ArchitectureGraphSnapshot.Empty;

	internal static event EventHandler? Changed;

	internal static ArchitectureGraphSnapshot Current => _current;

	internal static void Publish(ArchitectureEditorSnapshot snapshot)
	{
		_current = snapshot.GraphSnapshot;
		Changed?.Invoke(null, EventArgs.Empty);
	}

	internal static void Publish(ArchitectureGraphSnapshot snapshot)
	{
		_current = snapshot;
		Changed?.Invoke(null, EventArgs.Empty);
	}
}
