namespace RonSijm.AnaalIJzer.Application;

public enum ApplicationInputKind
{
	Project,
	Solution,
	ConfigurationFile
}

public enum ApplicationOperationKind
{
	GenerateConfig,
	ExportConfig,
	Documentation,
	Report,
	Inspect,
	MergeConfig,
	SplitConfig,
	FormatConfig,
	ExplainConfig
}

public enum ApplicationOutputKind
{
	File,
	Directory
}

public sealed record ApplicationOperationDefinition(
	ApplicationOperationKind Kind,
	string CommandName,
	string DisplayName,
	string Description,
	IReadOnlyList<ApplicationInputKind> SupportedInputs,
	ApplicationInputKind DefaultInput,
	string Usage,
	IReadOnlyList<string> Aliases)
{
	public int MinimumInputCount { get; init; } = 1;
	public int? MaximumInputCount { get; init; } = 1;
	public ApplicationOutputKind OutputKind { get; init; } = ApplicationOutputKind.File;

	public bool Supports(ApplicationInputKind inputKind)
	{
		var result = SupportedInputs.Contains(inputKind);

		return result;
	}

	public bool SupportsMultipleInputs
	{
		get { return MaximumInputCount is null || MaximumInputCount > 1; }
	}
}

public sealed record ApplicationInputDefinition(
	ApplicationInputKind Kind,
	string OptionName,
	string? ShortOption,
	string DisplayName,
	string Placeholder,
	string Description);

