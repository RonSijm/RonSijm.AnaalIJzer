namespace RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

public sealed class ArchitectureDiagnosticDefinition(
	string id,
	ArchitectureDiagnosticConcern concern,
	ArchitectureDiagnosticReason reason,
	string title,
	string documentationPath,
	ArchitectureDiagnosticDefaultSeverity defaultSeverity,
	ArchitectureDiagnosticSurface surface)
{
	public string Id { get; } = id;

	public ArchitectureDiagnosticConcern Concern { get; } = concern;

	public ArchitectureDiagnosticReason Reason { get; } = reason;

	public string Title { get; } = title;

	public string DocumentationPath { get; } = documentationPath;

	public ArchitectureDiagnosticDefaultSeverity DefaultSeverity { get; } = defaultSeverity;

	public ArchitectureDiagnosticSurface Surface { get; } = surface;

	public string Category
	{
		get
		{
			var result = $"Architecture.{Concern}";

			return result;
		}
	}
}

