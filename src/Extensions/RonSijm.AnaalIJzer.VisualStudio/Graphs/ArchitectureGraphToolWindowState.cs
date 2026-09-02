using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.VisualStudio.Graphs;

internal static class ArchitectureGraphToolWindowState
{
	private static ArchitectureGraphToolWindowContext _current = ArchitectureGraphToolWindowContext.Empty;

	internal static event EventHandler? Changed;

	internal static ArchitectureGraphSnapshot Current => _current.GraphSnapshot;

	internal static ArchitectureGraphToolWindowContext CurrentContext => _current;

	internal static void Publish(ArchitectureEditorSnapshot snapshot)
	{
		_current = new ArchitectureGraphToolWindowContext(snapshot.GraphSnapshot, null, null, null);
		Changed?.Invoke(null, EventArgs.Empty);
	}

	internal static void Publish(ArchitectureEditorSnapshot snapshot, string? documentPath, string? projectPath, string? solutionPath)
	{
		_current = new ArchitectureGraphToolWindowContext(snapshot.GraphSnapshot, documentPath, projectPath, solutionPath);
		Changed?.Invoke(null, EventArgs.Empty);
	}

	internal static void Publish(ArchitectureGraphToolWindowContext context)
	{
		_current = context;
		Changed?.Invoke(null, EventArgs.Empty);
	}

	internal static void Publish(ArchitectureGraphSnapshot snapshot)
	{
		_current = new ArchitectureGraphToolWindowContext(snapshot, _current.DocumentPath, _current.ProjectPath, _current.SolutionPath);
		Changed?.Invoke(null, EventArgs.Empty);
	}

	internal static void PublishDetached(ArchitectureGraphSnapshot snapshot)
	{
		_current = new ArchitectureGraphToolWindowContext(snapshot, null, null, null);
		Changed?.Invoke(null, EventArgs.Empty);
	}
}
