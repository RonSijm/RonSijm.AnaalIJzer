namespace RonSijm.IJzer.IaC;

public sealed class ArchitectureConfigurationExportOptions
{
    public bool Indent { get; set; } = true;

    public bool IncludeXmlDeclaration { get; set; } = true;

    public string? SchemaLocation { get; set; }
}
