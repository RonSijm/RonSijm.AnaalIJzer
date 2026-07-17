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
		ImmutableArray<ArchitectureConfigurationElementDetails> allowedPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> forbiddenPolicies)
	{
		Succeeded = succeeded;
		Message = message;
		Name = name;
		Description = description;
		RequireRecognizedDependencies = requireRecognizedDependencies;
		Matchers = matchers;
		AllowedPolicies = allowedPolicies;
		ForbiddenPolicies = forbiddenPolicies;
	}

	public bool Succeeded { get; }

	public string Message { get; }

	public string Name { get; }

	public string? Description { get; }

	public string? RequireRecognizedDependencies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> Matchers { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> AllowedPolicies { get; }

	public ImmutableArray<ArchitectureConfigurationElementDetails> ForbiddenPolicies { get; }

	public static ArchitectureLayerInspectionResult Success(
		string name,
		string? description,
		string? requireRecognizedDependencies,
		ImmutableArray<ArchitectureConfigurationElementDetails> matchers,
		ImmutableArray<ArchitectureConfigurationElementDetails> allowedPolicies,
		ImmutableArray<ArchitectureConfigurationElementDetails> forbiddenPolicies)
	{
		var result = new ArchitectureLayerInspectionResult(
			true,
			string.Empty,
			name,
			description,
			requireRecognizedDependencies,
			matchers,
			allowedPolicies,
			forbiddenPolicies);

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
			ImmutableArray<ArchitectureConfigurationElementDetails>.Empty);

		return result;
	}
}
