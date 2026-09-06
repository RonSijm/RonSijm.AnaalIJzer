namespace RonSijm.AnaalIJzer.Core.Observations;

public static class GeneratedCodeAnalysisModeParser
{
	public static bool TryParse(string? value, out GeneratedCodeAnalysisMode mode)
	{
		if (string.Equals(value, nameof(GeneratedCodeAnalysisMode.Exclude), StringComparison.OrdinalIgnoreCase))
		{
			mode = GeneratedCodeAnalysisMode.Exclude;
			return true;
		}

		if (string.Equals(value, nameof(GeneratedCodeAnalysisMode.IncludeConfigured), StringComparison.OrdinalIgnoreCase))
		{
			mode = GeneratedCodeAnalysisMode.IncludeConfigured;
			return true;
		}

		if (string.Equals(value, nameof(GeneratedCodeAnalysisMode.IncludeAll), StringComparison.OrdinalIgnoreCase))
		{
			mode = GeneratedCodeAnalysisMode.IncludeAll;
			return true;
		}

		mode = default;
		return false;
	}
}
