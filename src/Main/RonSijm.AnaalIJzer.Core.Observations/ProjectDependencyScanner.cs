using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public static partial class ProjectDependencyScanner
{
    public static IReadOnlyList<ProjectDependencyObservation> Scan(Compilation compilation, Func<INamedTypeSymbol, string?> resolveLayer, CancellationToken cancellationToken)
    {
        var result = Scan(compilation, resolveLayer, GeneratedCodeAnalysisScope.Exclude, cancellationToken);

        return result;
    }

    public static IReadOnlyList<ProjectDependencyObservation> Scan(Compilation compilation, Func<INamedTypeSymbol, string?> resolveLayer, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
    {
        var observations = new List<ProjectDependencyObservation>();
        foreach (var observation in DependencySiteObservationScanner.Scan(compilation, generatedCodeScope, cancellationToken))
        {
            var callerLayer = resolveLayer(observation.CallerType);
            if (callerLayer is null || observation.DependencyType.Name == observation.CallerType.Name)
            {
                continue;
            }

            var dependencyLayer = resolveLayer(observation.DependencyType);
            if (dependencyLayer is null)
            {
                continue;
            }

            observations.Add(new ProjectDependencyObservation(observation.CallerType, callerLayer, observation.DependencyType, dependencyLayer, observation.Site, observation.Location));
        }

        return observations;
    }
}