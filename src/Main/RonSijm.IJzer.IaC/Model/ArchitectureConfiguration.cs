namespace RonSijm.IJzer.IaC;

public sealed class ArchitectureConfiguration
{
    public Collection<IRootConfigurationElement> Elements { get; } = [];

    public Collection<DependencySite> RequireRecognizedDependencies { get; } = [];

    public bool EnforceAcyclic { get; set; }

    public bool EnforceObservedAcyclic { get; set; }

    public bool EnableReport { get; set; }

    public string? ReportPath { get; set; }

    public bool EnableDocumentation { get; set; }

    public string? DocumentationPath { get; set; }

    public string? Description { get; set; }

    public string ToXml(ArchitectureConfigurationExportOptions? options = null)
    {
        var result = ArchitectureConfigurationExporter.Export(this, options);

        return result;
    }

    public void Save(string path, ArchitectureConfigurationExportOptions? options = null)
    {
        ArchitectureConfigurationExporter.Save(this, path, options);
    }
}
