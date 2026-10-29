using RonSijm.IJzer.GraphModel.Model;

namespace RonSijm.IJzer.Outputs.GraphExports;

public interface IArchitectureGraphImageRenderer
{
    void ExportGraph(ArchitectureGraphSnapshot snapshot, string outputPath);

    void ExportPlaceholder(string outputPath, string title, string message);
}