namespace RonSijm.AnaalIJzer.Arse.Components;

internal static partial class PathSuggestionProvider
{
	private static (string Prefix, string Segment) SplitInput(string input, bool allowMultiple)
	{
		if (!allowMultiple)
		{
			return (string.Empty, input);
		}

		var separatorIndex = input.LastIndexOf(';');
		if (separatorIndex < 0)
		{
			return (string.Empty, input);
		}

		var segmentStart = separatorIndex + 1;
		while (segmentStart < input.Length && char.IsWhiteSpace(input[segmentStart]))
		{
			segmentStart++;
		}

		var result = (input[..segmentStart], input[segmentStart..]);

		return result;
	}
}
