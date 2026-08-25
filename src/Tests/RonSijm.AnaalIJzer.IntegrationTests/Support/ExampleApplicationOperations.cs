using AwesomeAssertions;
using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.IntegrationTests;

internal static class ExampleApplicationOperations
{
	public static async Task<string> GetXmlConfigurationForMergeAsync(ApplicationRunner runner, string projectPath, string relativeProjectPath, string tempDirectory, CancellationToken cancellationToken)
	{
		var projectDirectory = Path.GetDirectoryName(projectPath)!;
		var fileConfigurationPath = Path.Combine(projectDirectory, "Architecture.anl");
		if (File.Exists(fileConfigurationPath))
		{
			return fileConfigurationPath;
		}

		var exportDirectory = Path.Combine(tempDirectory, "ExportedInlineSettings");
		Directory.CreateDirectory(exportDirectory);
		var exportedConfigurationPath = Path.Combine(exportDirectory, Path.ChangeExtension(SanitizePath(relativeProjectPath), ".anl"));
		await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ExportConfig)
		{
			InputKind = ApplicationInputKind.Project,
			InputPaths = [projectPath],
			OutputPath = exportedConfigurationPath,
			Force = true
		}, cancellationToken);

		File.Exists(exportedConfigurationPath).Should().BeTrue($"inline settings from {relativeProjectPath} should be exported before merging");

		return exportedConfigurationPath;
	}

	private static string SanitizePath(string path)
	{
		var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
		var characters = path.Select(character => invalidCharacters.Contains(character) || character is '\\' or '/' or ':' ? '-' : character).ToArray();
		var result = new string(characters).Trim('-');

		return result;
	}
}
