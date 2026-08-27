using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Outputs.Inspection;

internal sealed class ArchitectureHealthReport(string markdown, int findingCount, ImmutableArray<ArchitectureFinding> findings)
{
	public string Markdown { get; } = markdown;

	public int FindingCount { get; } = findingCount;

	public ImmutableArray<ArchitectureFinding> Findings { get; } = findings;
}
