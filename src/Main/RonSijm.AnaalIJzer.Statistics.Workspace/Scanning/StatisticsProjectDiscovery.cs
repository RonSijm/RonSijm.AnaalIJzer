namespace RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;

public static class StatisticsProjectDiscovery
{
	private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
	{
		".git",
		".vs",
		"bin",
		"obj"
	};

	public static IReadOnlyList<string> FindProjects(string directoryPath)
	{
		var fullDirectoryPath = Path.GetFullPath(directoryPath);
		if (!Directory.Exists(fullDirectoryPath))
		{
			throw new DirectoryNotFoundException("Directory not found: " + fullDirectoryPath);
		}

		var projects = new List<string>();
		CollectProjects(fullDirectoryPath, projects);
		var result = projects
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		return result;
	}

	private static void CollectProjects(string directoryPath, List<string> projects)
	{
		foreach (var projectPath in Directory.EnumerateFiles(directoryPath, "*.csproj", SearchOption.TopDirectoryOnly))
		{
			projects.Add(Path.GetFullPath(projectPath));
		}

		foreach (var childDirectoryPath in Directory.EnumerateDirectories(directoryPath))
		{
			if (IgnoredDirectoryNames.Contains(Path.GetFileName(childDirectoryPath)))
			{
				continue;
			}

			CollectProjects(childDirectoryPath, projects);
		}
	}
}
