namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly partial struct ProjectReferenceRule
{
	public bool MatchesSourceProject(string projectName)
	{
		var result = FromMatchers.IsDefaultOrEmpty || FromMatchers.Any(matcher => matcher.Matches(projectName));

		return result;
	}

	public bool MatchesTargetProject(string projectName)
	{
		var result = ToMatchers.IsDefaultOrEmpty || ToMatchers.Any(matcher => matcher.Matches(projectName));

		return result;
	}
}
