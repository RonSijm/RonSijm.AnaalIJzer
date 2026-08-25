using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public sealed class ProjectDependencyObservation
{
	public ProjectDependencyObservation(
		INamedTypeSymbol callerType,
		string callerLayer,
		INamedTypeSymbol dependencyType,
		string dependencyLayer,
		string site,
		Location location)
	{
		CallerType = callerType;
		CallerLayer = callerLayer;
		DependencyType = dependencyType;
		DependencyLayer = dependencyLayer;
		Site = site;
		Location = location;
	}

	public INamedTypeSymbol CallerType { get; }

	public string CallerLayer { get; }

	public INamedTypeSymbol DependencyType { get; }

	public string DependencyLayer { get; }

	public string Site { get; }

	public Location Location { get; }

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
