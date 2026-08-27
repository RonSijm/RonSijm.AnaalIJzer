namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

internal sealed class ExampleRepositoryContext
{
	private ExampleRepositoryContext(string repositoryRoot)
	{
		RepositoryRoot = repositoryRoot;
		ExamplesRoot = Path.Combine(repositoryRoot, "Examples");
		SchemaPath = Path.Combine(repositoryRoot, "src", "Main", "RonSijm.AnaalIJzer", "Scheme", "AnaalIJzer.xsd");
	}

	public string RepositoryRoot { get; }

	public string ExamplesRoot { get; }

	public string SchemaPath { get; }

	public static ExampleRepositoryContext Discover()
	{
		var result = new ExampleRepositoryContext(FindRepositoryRoot());

		return result;
	}

	public string GetExampleProjectPath(string relativeProjectPath)
	{
		var projectName = Path.GetFileName(relativeProjectPath);
		var result = Path.Combine(ExamplesRoot, relativeProjectPath, projectName + ".csproj");

		return result;
	}

	public string[] FindAllExampleProjectPaths()
	{
		var result = Directory
			.EnumerateFiles(ExamplesRoot, "Example.*.csproj", SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		return result;
	}

	public string[] FindStandaloneConfigurationExamples()
	{
		var starterConfigDirectory = Path.Combine(ExamplesRoot, "StarterConfigs");
		var result = Directory.Exists(starterConfigDirectory)
			? Directory.EnumerateFiles(starterConfigDirectory, "*.anl", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
			: [];

		return result;
	}

	public string SanitizePath(string path)
	{
		var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
		var characters = path.Select(character => invalidCharacters.Contains(character) || character is '\\' or '/' or ':' ? '-' : character).ToArray();
		var result = new string(characters).Trim('-');

		return result;
	}

	private static string FindRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "RonSijm.AnaalIJzer.WithExamples.slnx")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Could not find repository root containing RonSijm.AnaalIJzer.WithExamples.slnx.");
	}
}
