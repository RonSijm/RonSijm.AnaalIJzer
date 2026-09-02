using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.VisualStudio.Graphs;

internal sealed class ArchitectureGraphToolWindowContext
{
	public ArchitectureGraphToolWindowContext(
		ArchitectureGraphSnapshot graphSnapshot,
		string? documentPath,
		string? projectPath,
		string? solutionPath)
	{
		GraphSnapshot = graphSnapshot;
		DocumentPath = documentPath;
		ProjectPath = projectPath;
		SolutionPath = solutionPath;
	}

	public ArchitectureGraphSnapshot GraphSnapshot { get; }

	public string? DocumentPath { get; }

	public string? ProjectPath { get; }

	public string? SolutionPath { get; }

	public bool HasWorkspaceContext
	{
		get
		{
			var result = !string.IsNullOrWhiteSpace(DocumentPath)
			             && !string.IsNullOrWhiteSpace(ProjectPath);

			return result;
		}
	}

	public static ArchitectureGraphToolWindowContext Empty { get; } =
		new(ArchitectureGraphSnapshot.Empty, null, null, null);
}
