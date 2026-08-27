using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Application;

public enum ConfigurationGenerationStrategy
{
	Snapshot,
	Helpful,
	Conventions
}

public sealed record ConfigurationGenerationOptions
{
	public ConfigurationGenerationStrategy Strategy { get; init; } = ConfigurationGenerationStrategy.Snapshot;
	public double MinimumConfidence { get; init; } = 0.90;
	public int MinimumSupport { get; init; } = 5;
}

public sealed record ApplicationRequest(ApplicationOperationKind Operation)
{
	public ApplicationInputKind? InputKind { get; init; }
	public IReadOnlyList<string> InputPaths { get; init; } = [];
	public string? OutputPath { get; init; }
	public string Configuration { get; init; } = "Release";
	public ConfigurationGenerationOptions GenerationOptions { get; init; } = new();
	public bool IncludeCodeEvidence { get; init; }
	public bool IncludeDocumentationInput { get; init; }
	public bool GenerateDocumentation { get; init; }
	public bool Force { get; init; }
	public bool WriteOutput { get; init; } = true;
}

public static class ApplicationInputPathParser
{
	public static IReadOnlyList<string> Parse(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

public sealed record ApplicationRunResult(
	string OutputPath,
	string Message,
	bool HasFindings = false,
	string? Content = null,
	ImmutableArray<ArchitectureFinding> Findings = default);

public sealed class ApplicationOperationException(string message) : Exception(message);

