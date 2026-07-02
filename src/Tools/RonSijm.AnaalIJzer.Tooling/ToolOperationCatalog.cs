namespace RonSijm.AnaalIJzer.Tooling;

public enum ToolInputKind
{
	Project,
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
	public bool Supports(ToolInputKind inputKind) => SupportedInputs.Contains(inputKind);
	public bool SupportsMultipleInputs => MaximumInputCount is null || MaximumInputCount > 1;
}

public static class ToolOperationCatalog
{
	public static IReadOnlyList<ToolOperationDefinition> All { get; } =
	[
		new(
			ToolOperationKind.GenerateConfig,
			"generate-config",
			"Generate XML settings",
			"Inspect a project and snapshot its structure or infer dominant architecture conventions.",
			[ToolInputKind.Project],
			ToolInputKind.Project,
			"generate-config --project <project.csproj> [--strategy <snapshot|conventions>] [--minimum-confidence <0..1>] [--minimum-support <count>] [--generate-documentation] [--include-input] [--output <ArchitecturalLevels.xml>] [--force]",
			["scaffold-config"]),
		new(
			ToolOperationKind.ExportConfig,
			"export-config",
			"Export inline settings",
			"Persist compiled AssemblyMetadata settings as an XML file.",
			[ToolInputKind.Project],
			ToolInputKind.Project,
			"export-config --project <project.csproj> [--output <ArchitecturalLevels.xml>] [--force]",
			[]),
		new(
			ToolOperationKind.Documentation,
			"documentation",
			"Generate documentation",
			"Generate architecture documentation from a project or XML settings file.",
			[ToolInputKind.ConfigurationFile, ToolInputKind.Project],
			ToolInputKind.ConfigurationFile,
			"documentation (--project <project.csproj> [--include-code-evidence] | --config <ArchitecturalLevels.xml>) [--include-input] [--output <architecture-documentation.md>] [--force]",
			["docs", "generate-documentation"]),
		new(
			ToolOperationKind.Report,
			"report",
			"Generate violation report",
			"Analyze a project and write its architecture violations as Markdown.",
			[ToolInputKind.Project],
			ToolInputKind.Project,
			"report --project <project.csproj> [--output <architectural-violations.md>] [--force]",
			["generate-report"]),
		new(
			ToolOperationKind.Inspect,
			"inspect",
			"Inspect architecture",
			"Find invalid settings, unclassified or ambiguous types, stale rules, unused edges, and dependency cycles.",
			[ToolInputKind.Project, ToolInputKind.ConfigurationFile],
			ToolInputKind.Project,
			"inspect (--project <project.csproj> | --config <ArchitecturalLevels.xml>) [--output <architecture-health.md>] [--force]",
			["validate", "doctor", "health"]),
		new(
			ToolOperationKind.MergeConfig,
			"merge-config",
			"Merge XML settings",
			"Flatten one or more XML settings files and their includes into one file.",
			[ToolInputKind.ConfigurationFile],
			ToolInputKind.ConfigurationFile,
			"merge-config --config <file.xml> [--config <file.xml> ...] [--output <merged.xml>] [--force]",
			["merge"])
		{
			MaximumInputCount = null
		},
		new(
			ToolOperationKind.SplitConfig,
			"split-config",
			"Split XML settings",
			"Extract disconnected dependency graphs into separate XML files.",
			[ToolInputKind.ConfigurationFile],
			ToolInputKind.ConfigurationFile,
			"split-config --config <file.xml> [--output <directory>] [--force]",
			["split"])
		{
			OutputKind = ToolOutputKind.Directory
		}
	];

	public static ToolOperationDefinition Get(ToolOperationKind kind) =>
		All.Single(operation => operation.Kind == kind);

	public static ToolOperationDefinition? Find(string commandName) =>
		All.FirstOrDefault(operation =>
			string.Equals(operation.CommandName, commandName, StringComparison.OrdinalIgnoreCase)
			|| operation.Aliases.Any(alias => string.Equals(alias, commandName, StringComparison.OrdinalIgnoreCase)));
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
			ToolInputKind.ConfigurationFile,
			"--config",
			null,
			"XML settings",
			"path\\to\\ArchitecturalLevels.xml",
			"XML settings file to document without loading a project.")
	];

	public static ToolInputDefinition Get(ToolInputKind kind) =>
		All.Single(input => input.Kind == kind);

	public static ToolInputDefinition? FindOption(string optionName) =>
		All.FirstOrDefault(input =>
			string.Equals(input.OptionName, optionName, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(input.ShortOption, optionName, StringComparison.OrdinalIgnoreCase));
}
