using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public interface IArchitectureGraphImageRenderer
{
	void ExportGraph(ArchitectureGraphSnapshot snapshot, string outputPath);

	void ExportPlaceholder(string outputPath, string title, string message);
}
