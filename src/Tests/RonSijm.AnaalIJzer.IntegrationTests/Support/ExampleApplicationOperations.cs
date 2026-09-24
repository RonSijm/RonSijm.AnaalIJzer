using AwesomeAssertions;
using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

internal static class ExampleApplicationOperations
{
    public static async Task<string> GetXmlConfigurationForMergeAsync(ApplicationRunner runner, string projectPath, string relativeProjectPath, string tempDirectory, CancellationToken cancellationToken)
    {
        var fileConfigurationPath = FindLinkedFileConfigurationPath(projectPath);
        if (fileConfigurationPath is not null)
        {
            return fileConfigurationPath;
        }

        var projectDirectory = Path.GetDirectoryName(projectPath)!;

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

    public static string? FindLinkedFileConfigurationPath(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var localConfigurationPath = Path.Combine(projectDirectory, "Architecture.anl");
        if (File.Exists(localConfigurationPath))
        {
            return localConfigurationPath;
        }

        // Examples may share one scenario-level configuration through the parent glob in Examples/Directory.Build.props.
        var parentConfigurationPath = Path.Combine(Directory.GetParent(projectDirectory)!.FullName, "Architecture.anl");
        if (File.Exists(parentConfigurationPath))
        {
            return parentConfigurationPath;
        }

        var projectDocument = System.Xml.Linq.XDocument.Load(projectPath);
        foreach (var include in projectDocument
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "AdditionalFiles", StringComparison.Ordinal))
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (include!.Contains('*') || !include.EndsWith(".anl", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidatePath = Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return null;
    }

    private static string SanitizePath(string path)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var characters = path.Select(character => invalidCharacters.Contains(character) || character is '\\' or '/' or ':' ? '-' : character).ToArray();
        var result = new string(characters).Trim('-');

        return result;
    }
}