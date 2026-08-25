using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public static class LocationExtensions
{
	public static string GetSourcePath(this Location? location)
	{
		if (location is null || !location.IsInSource)
		{
			return string.Empty;
		}

		var lineSpan = location.GetLineSpan();
		var result = string.IsNullOrWhiteSpace(lineSpan.Path)
			? location.SourceTree?.FilePath ?? string.Empty
			: lineSpan.Path;

		return result;
	}

	public static int GetSourceLineNumber(this Location? location)
	{
		if (location is null || !location.IsInSource)
		{
			return 0;
		}

		var lineSpan = location.GetLineSpan();
		var result = lineSpan.StartLinePosition.Line + 1;

		return result;
	}
}
