using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public sealed class ProjectDependencyObservation(
	INamedTypeSymbol callerType,
	string callerLayer,
	INamedTypeSymbol dependencyType,
	string dependencyLayer,
	string site,
	Location location)
{
	public INamedTypeSymbol CallerType { get; } = callerType;

	public string CallerLayer { get; } = callerLayer;

	public INamedTypeSymbol DependencyType { get; } = dependencyType;

	public string DependencyLayer { get; } = dependencyLayer;

	public string Site { get; } = site;

	public Location Location { get; } = location;

	public ObservedDependency ToObservedDependency(string? sourceProjectName = null)
	{
		var result = new ObservedDependency(
			CallerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
			CallerLayer,
			DependencyType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
			DependencyLayer,
			Site,
			Location,
			sourceProjectName);

		return result;
	}
}
