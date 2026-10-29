using RonSijm.IJzer.GraphModel.Model;

namespace RonSijm.IJzer.Outputs.GraphExports;

public interface IArchitectureGraphSnapshotLoader
{
    Task<ArchitectureGraphSnapshot> LoadAsync(string inputPath, CancellationToken cancellationToken);
}