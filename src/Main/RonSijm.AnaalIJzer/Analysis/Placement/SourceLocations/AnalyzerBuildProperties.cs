using Microsoft.CodeAnalysis.Diagnostics;

namespace RonSijm.AnaalIJzer.Analysis.SourceLocations;

internal readonly struct AnalyzerBuildProperties(string? projectDirectory)
{
	internal const string MsBuildProjectDirectoryPropertyName = "build_property.MSBuildProjectDirectory";

	public string? ProjectDirectory { get; } = projectDirectory;

	public static AnalyzerBuildProperties Read(AnalyzerConfigOptionsProvider optionsProvider)
	{
		optionsProvider.GlobalOptions.TryGetValue(MsBuildProjectDirectoryPropertyName, out var projectDirectory);
		var result = new AnalyzerBuildProperties(string.IsNullOrWhiteSpace(projectDirectory) ? null : projectDirectory);

		return result;
	}
}
