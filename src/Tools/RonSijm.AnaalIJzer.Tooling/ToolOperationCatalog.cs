namespace RonSijm.AnaalIJzer.Tooling;

public enum ToolInputKind
{
	Project,
	Solution,
	ConfigurationFile
}

public enum ToolOperationKind
{
	GenerateConfig,
	ExportConfig,
	Documentation,
	Report,
	Inspect,
	MergeConfig,
	SplitConfig
}

public enum ToolOutputKind
{
	File,
	Directory
}

public sealed record ToolOperationDefinition(
	ToolOperationKind Kind,
	string CommandName,
	string DisplayName,
	string Description,
	IReadOnlyList<ToolInputKind> SupportedInputs,
	ToolInputKind DefaultInput,
	string Usage,
	IReadOnlyList<string> Aliases)
{
	public int MinimumInputCount { get; init; } = 1;
	public int? MaximumInputCount { get; init; } = 1;
	public ToolOutputKind OutputKind { get; init; } = ToolOutputKind.File;
	public bool Supports(ToolInputKind inputKind)
	{
		var result = SupportedInputs.Contains(inputKind);

		return result;
	}

	public bool SupportsMultipleInputs
	{
		get { return MaximumInputCount is null || MaximumInputCount > 1; }
	}
}

public static class ToolOperationCatalog
{
	public static IReadOnlyList<ToolOperationDefinition> All { get; } =
	[
		new(
			ToolOperationKind.GenerateConfig,
			"generate-config",
			"Generate architecture settings",
			"Inspect a project or solution and snapshot its structure, create a helpful baseline, or infer dominant architecture conventions.",
			[ToolInputKind.Project, ToolInputKind.Solution],
			ToolInputKind.Project,
			"generate-config (--project <project.csproj> | --solution <solution.slnx>) [--strategy <snapshot|helpful|conventions>] [--minimum-confidence <0..1>] [--minimum-support <count>] [--generate-documentation] [--include-input] [--output <Architecture.anl>] [--force]",
			["scaffold-config"]),
		new(
			ToolOperationKind.ExportConfig,
			"export-config",
			"Export inline settings",
			"Persist compiled AssemblyMetadata settings as an XML file.",
			[ToolInputKind.Project],
			ToolInputKind.Project,
			"export-config --project <project.csproj> [--output <Architecture.anl>] [--force]",
			[]),
		new(
			ToolOperationKind.Documentation,
			"documentation",
			"Generate documentation",
			"Generate architecture documentation from a project or architecture settings file.",
			[ToolInputKind.ConfigurationFile, ToolInputKind.Project],
			ToolInputKind.ConfigurationFile,
			"documentation (--project <project.csproj> [--include-code-evidence] | --config <Architecture.anl>) [--include-input] [--output <architecture-documentation.md>] [--force]",
			["docs", "generate-documentation"]),
		new(
			ToolOperationKind.Report,
			"report",
			"Generate violation report",
			"Analyze a project or solution and write its architecture violations as Markdown.",
			[ToolInputKind.Project, ToolInputKind.Solution],
			ToolInputKind.Project,
			"report (--project <project.csproj> | --solution <solution.slnx>) [--output <architectural-violations.md>] [--force]",
			["generate-report"]),
		new(
			ToolOperationKind.Inspect,
			"inspect",
			"Inspect architecture",
			"Find invalid settings, unclassified or ambiguous types, stale rules, unused edges, and dependency cycles.",
			[ToolInputKind.Project, ToolInputKind.Solution, ToolInputKind.ConfigurationFile],
			ToolInputKind.Project,
			"inspect (--project <project.csproj> | --solution <solution.slnx> | --config <Architecture.anl>) [--output <architecture-health.md>] [--force]",
			["validate", "doctor", "health"]),
		new(
			ToolOperationKind.MergeConfig,
			"merge-config",
			"Merge architecture settings",
			"Flatten one or more architecture settings files and their includes into one file.",
			[ToolInputKind.ConfigurationFile],
			ToolInputKind.ConfigurationFile,
			"merge-config --config <file.anl> [--config <file.anl> ...] [--output <merged.anl>] [--force]",
			["merge"])
		{
			MaximumInputCount = null
		},
		new(
			ToolOperationKind.SplitConfig,
			"split-config",
			"Split architecture settings",
			"Extract disconnected dependency graphs into separate architecture settings files.",
			[ToolInputKind.ConfigurationFile],
			ToolInputKind.ConfigurationFile,
			"split-config --config <file.anl> [--output <directory>] [--force]",
			["split"])
		{
			OutputKind = ToolOutputKind.Directory
		}
	];

	public static ToolOperationDefinition Get(ToolOperationKind kind)
	{
		var result = All.Single(operation => operation.Kind == kind);

		return result;
	}

    public static ToolOperationDefinition? Find(string commandName)
    {
        return All.FirstOrDefault(operation =>
            string.Equals(operation.CommandName, commandName, StringComparison.OrdinalIgnoreCase)
            || operation.Aliases.Any(alias => string.Equals(alias, commandName, StringComparison.OrdinalIgnoreCase)));
    }
}

public sealed record ToolInputDefinition(
	ToolInputKind Kind,
	string OptionName,
	string? ShortOption,
	string DisplayName,
	string Placeholder,
	string Description);

public static class ToolInputCatalog
{
	public static IReadOnlyList<ToolInputDefinition> All { get; } =
	[
		new(
			ToolInputKind.Project,
			"--project",
			"-p",
			"Project",
			"path\\to\\Project.csproj",
			"Project file to load with MSBuildWorkspace."),
		new(
			ToolInputKind.Solution,
			"--solution",
			"-s",
			"Solution",
			"path\\to\\Solution.slnx",
			"Solution file to load with MSBuildWorkspace."),
		new(
			ToolInputKind.ConfigurationFile,
			"--config",
			null,
			"Architecture settings",
			"path\\to\\Architecture.anl",
			"Architecture settings file to document without loading a project.")
	];

	public static ToolInputDefinition Get(ToolInputKind kind)
	{
		var result = All.Single(input => input.Kind == kind);

		return result;
	}

    public static ToolInputDefinition? FindOption(string optionName)
    {
        return All.FirstOrDefault(input =>
            string.Equals(input.OptionName, optionName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(input.ShortOption, optionName, StringComparison.OrdinalIgnoreCase));
    }
}
