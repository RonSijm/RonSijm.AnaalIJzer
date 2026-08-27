using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphModel.Model;

public sealed class ArchitectureGraphSnapshot(
    bool hasConfiguration,
    bool hasConfigurationIssues,
    ImmutableArray<ArchitectureGraphLayer> layers,
    ImmutableArray<ArchitectureGraphRule> rules,
    ImmutableArray<string> activeLayerPaths,
    ImmutableArray<string> configurationIssueMessages,
    ArchitectureConfigurationSource? configurationSource = null,
    ArchitectureGraphEvidence? evidence = null,
    ImmutableArray<ArchitectureConfigurationCreationTarget> configurationCreationTargets = default,
    ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews = default)
{
    public bool HasConfiguration { get; } = hasConfiguration;

    public bool HasConfigurationIssues { get; } = hasConfigurationIssues;

    public ImmutableArray<ArchitectureGraphLayer> Layers { get; } = layers;

    public ImmutableArray<ArchitectureGraphRule> Rules { get; } = rules;

    public ImmutableArray<ArchitectureGraphExceptionReview> ExceptionReviews { get; } = exceptionReviews.IsDefault ? ImmutableArray<ArchitectureGraphExceptionReview>.Empty : exceptionReviews;

    public ImmutableArray<string> ActiveLayerPaths { get; } = activeLayerPaths;

    public ImmutableArray<string> ConfigurationIssueMessages { get; } = configurationIssueMessages;

    public ArchitectureConfigurationSource ConfigurationSource { get; } = configurationSource ?? ArchitectureConfigurationSource.None;

    public ArchitectureGraphEvidence Evidence { get; } = evidence ?? ArchitectureGraphEvidence.Empty;

    public ImmutableArray<ArchitectureConfigurationCreationTarget> ConfigurationCreationTargets { get; } = configurationCreationTargets.IsDefault ? ImmutableArray<ArchitectureConfigurationCreationTarget>.Empty : configurationCreationTargets;

    public static ArchitectureGraphSnapshot Empty { get; } = new(false, false, ImmutableArray<ArchitectureGraphLayer>.Empty, ImmutableArray<ArchitectureGraphRule>.Empty, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, exceptionReviews: ImmutableArray<ArchitectureGraphExceptionReview>.Empty);
}

