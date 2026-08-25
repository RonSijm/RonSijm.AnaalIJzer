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
		bool enableExceptionPolicy,
		bool requireExceptionReason,
		bool requireExceptionOwner,
		bool requireExceptionExpiresOn,
		int exceptionWarnBeforeDays,
		string? exceptionPolicyDescription,
		ImmutableArray<ArchitectureConfigurationElementDetails> includes,
		ImmutableArray<ArchitectureConfigurationElementDetails> exceptionMatchers,
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
		EnableExceptionPolicy = enableExceptionPolicy;
		RequireExceptionReason = requireExceptionReason;
		RequireExceptionOwner = requireExceptionOwner;
		RequireExceptionExpiresOn = requireExceptionExpiresOn;
		ExceptionWarnBeforeDays = exceptionWarnBeforeDays;
		ExceptionPolicyDescription = exceptionPolicyDescription;
		Includes = includes;
		ExceptionMatchers = exceptionMatchers;
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

	public bool EnableExceptionPolicy { get; }

	public bool RequireExceptionReason { get; }

	public bool RequireExceptionOwner { get; }

	public bool RequireExceptionExpiresOn { get; }

	public int ExceptionWarnBeforeDays { get; }

	public string? ExceptionPolicyDescription { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> Includes { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ExceptionMatchers { get; }

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
		bool enableExceptionPolicy,
		bool requireExceptionReason,
		bool requireExceptionOwner,
		bool requireExceptionExpiresOn,
		int exceptionWarnBeforeDays,
		string? exceptionPolicyDescription,
		ImmutableArray<ArchitectureConfigurationElementDetails> includes,
		ImmutableArray<ArchitectureConfigurationElementDetails> exceptionMatchers,
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
			enableExceptionPolicy,
			requireExceptionReason,
			requireExceptionOwner,
			requireExceptionExpiresOn,
			exceptionWarnBeforeDays,
			exceptionPolicyDescription,
			includes,
			exceptionMatchers,
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
			false,
			false,
			false,
			false,
			14,
			null,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty);

		return result;
	}
}
