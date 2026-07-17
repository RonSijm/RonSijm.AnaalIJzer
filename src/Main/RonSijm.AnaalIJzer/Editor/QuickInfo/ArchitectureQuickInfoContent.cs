using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.QuickInfo;

public sealed class ArchitectureQuickInfoContent(string title, ImmutableArray<string> lines)
{
    public string Title { get; } = title;

    public ImmutableArray<string> Lines { get; } = lines;

    public override string ToString()
	{
		var result = Title + "\n" + string.Join("\n", Lines);

		return result;
	}
}
