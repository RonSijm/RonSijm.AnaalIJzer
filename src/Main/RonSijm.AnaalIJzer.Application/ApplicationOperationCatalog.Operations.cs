namespace RonSijm.AnaalIJzer.Application;

public static class ApplicationOperationCatalog
{
	public static IReadOnlyList<ApplicationOperationDefinition> All { get; } =
	[
		new(
			ApplicationOperationKind.GenerateConfig,
			"generate-config",
			"Generate architecture settings",
			"Inspect a project or solution and snapshot its structure, create a helpful baseline, or infer dominant architecture conventions.",
			[ApplicationInputKind.Project, ApplicationInputKind.Solution],
			ApplicationInputKind.Project,
			"generate-config (--project <project.csproj> | --solution <solution.slnx>) [--strategy <snapshot|helpful|conventions>] [--minimum-confidence <0..1>] [--minimum-support <count>] [--generate-documentation] [--include-input] [--output <Architecture.anl>] [--force]",
			["scaffold-config"]),
		new(
			ApplicationOperationKind.ExportConfig,
			"export-config",
			"Export inline settings",
			"Persist compiled AssemblyMetadata settings as an XML file.",
			[ApplicationInputKind.Project],
			ApplicationInputKind.Project,
			"export-config --project <project.csproj> [--output <Architecture.anl>] [--force]",
			[]),
		new(
			ApplicationOperationKind.Documentation,
			"documentation",
			"Generate documentation",
			"Generate architecture documentation from a project or architecture settings file.",
			[ApplicationInputKind.ConfigurationFile, ApplicationInputKind.Project],
			ApplicationInputKind.ConfigurationFile,
			"documentation (--project <project.csproj> [--include-code-evidence] | --config <Architecture.anl>) [--include-input] [--output <architecture-documentation.md>] [--force]",
			["docs", "generate-documentation"]),
		new(
			ApplicationOperationKind.Report,
			"report",
			"Generate violation report",
			"Analyze a project or solution and write its architecture violations as Markdown.",
			[ApplicationInputKind.Project, ApplicationInputKind.Solution],
			ApplicationInputKind.Project,
			"report (--project <project.csproj> | --solution <solution.slnx>) [--output <architectural-violations.md>] [--force]",
			["generate-report"]),
		new(
			ApplicationOperationKind.Inspect,
			"inspect",
			"Inspect architecture",
			"Find invalid settings, unclassified or ambiguous types, stale rules, unused edges, and dependency cycles.",
			[ApplicationInputKind.Project, ApplicationInputKind.Solution, ApplicationInputKind.ConfigurationFile],
			ApplicationInputKind.Project,
			"inspect (--project <project.csproj> | --solution <solution.slnx> | --config <Architecture.anl>) [--output <architecture-health.md>] [--force]",
			["validate", "doctor", "health", "self-check"]),
		new(
			ApplicationOperationKind.Fixes,
			"fixes",
			"Find configuration fixes",
			"List configuration-backed code-fix proposals for architecture diagnostics and preview the config changes they would make.",
			[ApplicationInputKind.Project, ApplicationInputKind.Solution],
			ApplicationInputKind.Project,
			"fixes (--project <project.csproj> | --solution <solution.slnx>) [--output <architecture-fixes.md>] [--force]",
			["list-fixes", "config-fixes"]),
		new(
			ApplicationOperationKind.ApplyFix,
			"apply-fix",
			"Apply configuration fix",
			"Apply one configuration-backed fix proposal by id.",
			[ApplicationInputKind.Project, ApplicationInputKind.Solution],
			ApplicationInputKind.Project,
			"apply-fix (--project <project.csproj> | --solution <solution.slnx>) --fix-id <proposal-id>",
			[]),
		new(
			ApplicationOperationKind.MergeConfig,
			"merge-config",
			"Merge architecture settings",
			"Flatten one or more architecture settings files and their includes into one file.",
			[ApplicationInputKind.ConfigurationFile],
			ApplicationInputKind.ConfigurationFile,
			"merge-config --config <file.anl> [--config <file.anl> ...] [--output <merged.anl>] [--force]",
			["merge"])
		{
			MaximumInputCount = null
		},
		new(
			ApplicationOperationKind.SplitConfig,
			"split-config",
			"Split architecture settings",
			"Extract disconnected dependency graphs into separate architecture settings files.",
			[ApplicationInputKind.ConfigurationFile],
			ApplicationInputKind.ConfigurationFile,
			"split-config --config <file.anl> [--output <directory>] [--force]",
			["split"])
		{
			OutputKind = ApplicationOutputKind.Directory
		},
		new(
			ApplicationOperationKind.FormatConfig,
			"format-config",
			"Format architecture settings",
			"Normalize architecture settings XML formatting.",
			[ApplicationInputKind.ConfigurationFile],
			ApplicationInputKind.ConfigurationFile,
			"format-config --config <file.anl> [--output <file.anl>] [--force]",
			["format"]),
		new(
			ApplicationOperationKind.ExplainConfig,
			"explain-config",
			"Explain architecture settings",
			"Write a compact Markdown explanation of an architecture settings file.",
			[ApplicationInputKind.ConfigurationFile],
			ApplicationInputKind.ConfigurationFile,
			"explain-config --config <file.anl> [--output <architecture-explanation.md>] [--force]",
			["explain"])
	];

	public static ApplicationOperationDefinition Get(ApplicationOperationKind kind)
	{
		var result = All.Single(operation => operation.Kind == kind);

		return result;
	}

	public static ApplicationOperationDefinition? Find(string commandName)
	{
		var result = All.FirstOrDefault(operation =>
			string.Equals(operation.CommandName, commandName, StringComparison.OrdinalIgnoreCase)
			|| operation.Aliases.Any(alias => string.Equals(alias, commandName, StringComparison.OrdinalIgnoreCase)));

		return result;
	}
}

