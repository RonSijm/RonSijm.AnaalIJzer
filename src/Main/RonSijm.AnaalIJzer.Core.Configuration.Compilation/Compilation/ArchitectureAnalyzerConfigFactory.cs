using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.Policies;
using RonSijm.AnaalIJzer.Model;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Config.Compilation;

internal static class ArchitectureAnalyzerConfigFactory
{
    internal static AnalyzerConfig Create(
        ArchitectureConfigurationDocumentParseContext documentContext,
        ArchitectureConfigurationRootSettings rootSettings,
        ArchitectureConfigurationMaterializationResult materialization,
        ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var dependencyGraph = new DependencyGraph(materialization.DependencyEdges);
        var documentation = new ArchitectureDocumentation(
            documentContext.Documents
                .Select(document => document.Root.Attribute("description")?.Value)
                .FirstOrDefault(description => !string.IsNullOrWhiteSpace(description)),
            documentContext.DocumentationItems);
        var compiledConfig = new CompiledArchitectureConfig(
            materialization.LayerCatalog,
            dependencyGraph,
            rootSettings.Output,
            rootSettings.RequiredRecognizedDependencySites,
            materialization.LayerRequiredRecognizedDependencySites,
            rootSettings.ExceptionPolicy,
            materialization.ExceptionDefinitions,
            materialization.ExceptionReviews,
            rootSettings.EnforceAcyclic,
            rootSettings.EnforceObservedAcyclic,
            materialization.LayerNames,
            materialization.ForbiddenPatterns.Select(pattern => (pattern.Name, pattern.Comment)).ToImmutableArray(),
            materialization.ProjectArchitecture,
            documentation,
            issues.ToImmutable());
        var result = new AnalyzerConfig(compiledConfig);

        return result;
    }
}
