namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind kind, string path)
{
    public ArchitectureConfigurationSourceKind Kind { get; } = kind;

    public string Path { get; } = path;

    public bool CanEdit
	{
		get
		{
			var result = Kind != ArchitectureConfigurationSourceKind.None && !string.IsNullOrWhiteSpace(Path);

			return result;
		}
	}

	public static ArchitectureConfigurationSource None { get; } = new(ArchitectureConfigurationSourceKind.None, string.Empty);
}
