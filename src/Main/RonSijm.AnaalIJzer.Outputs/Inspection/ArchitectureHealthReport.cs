using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Findings;

namespace RonSijm.AnaalIJzer.Outputs.Inspection;

internal sealed class ArchitectureHealthReport
{
	public ArchitectureHealthReport(string markdown, int findingCount, ImmutableArray<ArchitectureFinding> findings)
	{
		Markdown = markdown;
		FindingCount = findingCount;
		Findings = findings;
	}

	public string Markdown { get; }

	public int FindingCount { get; }

	public ImmutableArray<ArchitectureFinding> Findings { get; }
}
