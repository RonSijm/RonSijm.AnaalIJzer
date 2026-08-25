namespace RonSijm.AnaalIJzer.Application;

public static class ApplicationInputCatalog
{
	public static IReadOnlyList<ApplicationInputDefinition> All { get; } =
	[
		new(
			ApplicationInputKind.Project,
			"--project",
			"-p",
			"Project",
			"path\\to\\Project.csproj",
			"Project file to load with MSBuildWorkspace."),
		new(
			ApplicationInputKind.Solution,
			"--solution",
			"-s",
			"Solution",
			"path\\to\\Solution.slnx",
			"Solution file to load with MSBuildWorkspace."),
		new(
			ApplicationInputKind.ConfigurationFile,
			"--config",
			null,
			"Architecture settings",
			"path\\to\\Architecture.anl",
			"Architecture settings file to document without loading a project.")
	];

	public static ApplicationInputDefinition Get(ApplicationInputKind kind)
	{
		var result = All.Single(input => input.Kind == kind);

		return result;
	}

	public static ApplicationInputDefinition? FindOption(string optionName)
	{
		var result = All.FirstOrDefault(input =>
			string.Equals(input.OptionName, optionName, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(input.ShortOption, optionName, StringComparison.OrdinalIgnoreCase));

		return result;
	}
}

