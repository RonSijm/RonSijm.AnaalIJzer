namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal static class WorkspaceFailureFilter
{
	public static bool IsIgnorable(string diagnosticText)
	{
		var result = diagnosticText.Contains("Audit source 'nuget.org' did not provide any vulnerability data.", StringComparison.Ordinal)
			|| diagnosticText.Contains("Error occurred while getting package vulnerability data:", StringComparison.Ordinal);

		return result;
	}
}
