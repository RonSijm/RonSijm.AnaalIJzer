using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureRootInspectionResult
{
	private ArchitectureRootInspectionResult(
		bool succeeded,
		string message,
		string? description,
		string? requireRecognizedDependencies,
		bool enforceAcyclic,
		bool enableReport,
		string? reportPath,
		bool enableDocumentation,
		string? documentationPath,
		ImmutableArray<ArchitectureConfigurationElementDetails> includes,
		ImmutableArray<ArchitectureConfigurationElementDetails> allowedPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> forbiddenPolicies)
	{
		Succeeded = succeeded;
		Message = message;
		Description = description;
		RequireRecognizedDependencies = requireRecognizedDependencies;
		EnforceAcyclic = enforceAcyclic;
		EnableReport = enableReport;
		ReportPath = reportPath;
		EnableDocumentation = enableDocumentation;
		DocumentationPath = documentationPath;
		Includes = includes;
		AllowedPolicies = allowedPolicies;
		ForbiddenPolicies = forbiddenPolicies;
	}

	public bool Succeeded { get; }

	public string Message { get; }

	public string? Description { get; }

	public string? RequireRecognizedDependencies { get; }

	public bool EnforceAcyclic { get; }

	public bool EnableReport { get; }

	public string? ReportPath { get; }

	public bool EnableDocumentation { get; }

	public string? DocumentationPath { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> Includes { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> AllowedPolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ForbiddenPolicies { get; }

	public static ArchitectureRootInspectionResult Success(
		string? description,
		string? requireRecognizedDependencies,
		bool enforceAcyclic,
		bool enableReport,
		string? reportPath,
		bool enableDocumentation,
		string? documentationPath,
		ImmutableArray<ArchitectureConfigurationElementDetails> includes,
		ImmutableArray<ArchitectureConfigurationElementDetails> allowedPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> forbiddenPolicies)
	{
		var result = new ArchitectureRootInspectionResult(
			true,
			string.Empty,
			description,
			requireRecognizedDependencies,
			enforceAcyclic,
			enableReport,
			reportPath,
			enableDocumentation,
			documentationPath,
			includes,
			allowedPolicies,
			forbiddenPolicies);

		return result;
	}

	public static ArchitectureRootInspectionResult Failure(string message)
	{
		var result = new ArchitectureRootInspectionResult(
			false,
			message,
			null,
			null,
			false,
			false,
			null,
			false,
			null,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty);

		return result;
	}
}
