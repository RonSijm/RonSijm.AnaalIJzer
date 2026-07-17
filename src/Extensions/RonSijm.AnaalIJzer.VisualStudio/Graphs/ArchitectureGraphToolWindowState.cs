using RonSijm.AnaalIJzer.Graphing;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio;

internal static class ArchitectureGraphToolWindowState
{
	private static ArchitectureGraphSnapshot current = ArchitectureGraphSnapshot.Empty;

	internal static event EventHandler? Changed;

	internal static ArchitectureGraphSnapshot Current => current;

	internal static void Publish(ArchitectureEditorSnapshot snapshot)
	{
		current = ArchitectureGraphSnapshotAdapter.FromEditorSnapshot(snapshot);
		Changed?.Invoke(null, EventArgs.Empty);
	}

	internal static void Publish(ArchitectureGraphSnapshot snapshot)
	{
		current = snapshot;
		Changed?.Invoke(null, EventArgs.Empty);
	}
}
