namespace RonSijm.AnaalIJzer.IntegrationTests;

internal sealed class GeneratedExampleFilesScope : IDisposable
{
	private readonly IReadOnlyDictionary<string, byte[]?> snapshots;

	public GeneratedExampleFilesScope(ExampleRepositoryContext context)
	{
		snapshots = SnapshotGeneratedExampleFiles(context.RepositoryRoot);
	}

	public void Dispose()
	{
		RestoreGeneratedExampleFiles(snapshots);
	}

	private static Dictionary<string, byte[]?> SnapshotGeneratedExampleFiles(string repositoryRoot)
	{
		var paths = new[]
		{
			Path.Combine(repositoryRoot, "Examples", "Documentation", "Generated", "architectural-violations.md"),
			Path.Combine(repositoryRoot, "Examples", "Documentation", "Generated", "architecture-documentation.md")
		};
		var result = paths.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null, StringComparer.OrdinalIgnoreCase);

		return result;
	}

	private static void RestoreGeneratedExampleFiles(IReadOnlyDictionary<string, byte[]?> snapshots)
	{
		foreach (var (path, contents) in snapshots)
		{
			if (contents is null)
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}

				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllBytes(path, contents);
		}
	}
}
