using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureLayerInspectionResult
{
	private ArchitectureLayerInspectionResult(
		bool succeeded,
		string message,
		string name,
		string? description,
		string? requireRecognizedDependencies,
		ImmutableArray<ArchitectureConfigurationElementDetails> matchers,
		ImmutableArray<ArchitectureConfigurationElementDetails> exceptionMatchers,
		ImmutableArray<ArchitectureConfigurationElementDetails> allowedPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> forbiddenPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> nameRules,
		ImmutableArray<ArchitectureConfigurationElementDetails> inheritancePolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> returnValuePolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> visibilityPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> apiSurfacePolicies)
	{
		Succeeded = succeeded;
		Message = message;
		Name = name;
		Description = description;
		RequireRecognizedDependencies = requireRecognizedDependencies;
		Matchers = matchers;
		ExceptionMatchers = exceptionMatchers;
		AllowedPolicies = allowedPolicies;
		ForbiddenPolicies = forbiddenPolicies;
		NameRules = nameRules;
		InheritancePolicies = inheritancePolicies;
		ReturnValuePolicies = returnValuePolicies;
		VisibilityPolicies = visibilityPolicies;
		ApiSurfacePolicies = apiSurfacePolicies;
	}

	public bool Succeeded { get; }

	public string Message { get; }

	public string Name { get; }

	public string? Description { get; }

	public string? RequireRecognizedDependencies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> Matchers { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ExceptionMatchers { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> AllowedPolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ForbiddenPolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> NameRules { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> InheritancePolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ReturnValuePolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> VisibilityPolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ApiSurfacePolicies { get; }

	public static ArchitectureLayerInspectionResult Success(
		string name,
		string? description,
		string? requireRecognizedDependencies,
		ImmutableArray<ArchitectureConfigurationElementDetails> matchers,
		ImmutableArray<ArchitectureConfigurationElementDetails> exceptionMatchers,
		ImmutableArray<ArchitectureConfigurationElementDetails> allowedPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> forbiddenPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> nameRules,
		ImmutableArray<ArchitectureConfigurationElementDetails> inheritancePolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> returnValuePolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> visibilityPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> apiSurfacePolicies)
	{
		var result = new ArchitectureLayerInspectionResult(
			true,
			string.Empty,
			name,
			description,
			requireRecognizedDependencies,
			matchers,
			exceptionMatchers,
			allowedPolicies,
			forbiddenPolicies,
			nameRules,
			inheritancePolicies,
			returnValuePolicies,
			visibilityPolicies,
			apiSurfacePolicies);

		return result;
	}

	public static ArchitectureLayerInspectionResult Failure(string message)
	{
		var result = new ArchitectureLayerInspectionResult(
			false,
			message,
			string.Empty,
			null,
			null,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty,
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty);

		return result;
	}
}
