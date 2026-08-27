using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public interface IArchitectureGraphSnapshotLoader
{
	Task<ArchitectureGraphSnapshot> LoadAsync(string inputPath, CancellationToken cancellationToken);
}
