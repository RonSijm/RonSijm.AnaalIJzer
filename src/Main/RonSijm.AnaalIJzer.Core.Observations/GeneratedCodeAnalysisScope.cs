using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public readonly struct GeneratedCodeAnalysisScope(
    GeneratedCodeAnalysisMode mode,
    ImmutableArray<GeneratedCodePathRule> configuredPaths,
    int maximumDocumentLength = 262_144)
{
    public const int DefaultMaximumDocumentLength = 262_144;
    public const int MaximumSupportedDocumentLength = 4_194_304;

    public static readonly GeneratedCodeAnalysisScope Exclude = new(
        GeneratedCodeAnalysisMode.Exclude,
        ImmutableArray<GeneratedCodePathRule>.Empty);

    public GeneratedCodeAnalysisMode Mode { get; } = mode;

    public ImmutableArray<GeneratedCodePathRule> ConfiguredPaths { get; } = configuredPaths;

    public int MaximumDocumentLength { get; } = maximumDocumentLength;

    public bool ShouldAnalyze(SyntaxTree syntaxTree, CancellationToken cancellationToken)
    {
        if (!GeneratedCodeDetector.IsGenerated(syntaxTree, cancellationToken))
        {
            return true;
        }

        if (Mode == GeneratedCodeAnalysisMode.Exclude)
        {
            return false;
        }

        var text = syntaxTree.GetText(cancellationToken);
        if (MaximumDocumentLength > 0 && text.Length > MaximumDocumentLength)
        {
            return false;
        }

        if (Mode == GeneratedCodeAnalysisMode.IncludeAll)
        {
            return true;
        }

        if (Mode != GeneratedCodeAnalysisMode.IncludeConfigured)
        {
            return false;
        }

        foreach (var configuredPath in ConfiguredPaths)
        {
            if (configuredPath.Matches(syntaxTree.FilePath))
            {
                return true;
            }
        }

        return false;
    }
}