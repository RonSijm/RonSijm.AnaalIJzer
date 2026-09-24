namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
    [Fact]
    public async Task ApplicationRunner_ReportsDiagnosticIdMigrationsWithoutChangingInputFiles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-diagnostic-id-migration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var projectPath = Path.Combine(tempDirectory, "MigrationSample.csproj");
            var sourcePath = Path.Combine(tempDirectory, "Sample.cs");
            var editorConfigPath = Path.Combine(tempDirectory, ".editorconfig");
            var outputPath = Path.Combine(tempDirectory, "migration.md");
            const string source = "#pragma warning disable ARCH022\npublic sealed class Sample { }\n";
            const string editorConfig = "[*.cs]\ndotnet_diagnostic.ARCH001.severity = none\n";
            await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />", cancellationToken);
            await File.WriteAllTextAsync(sourcePath, source, cancellationToken);
            await File.WriteAllTextAsync(editorConfigPath, editorConfig, cancellationToken);

            var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.MigrateDiagnosticIds)
            {
                InputKind = ApplicationInputKind.Project,
                InputPaths = [projectPath],
                OutputPath = outputPath
            }, cancellationToken);

            result.HasFindings.Should().BeTrue();
            result.Content.Should().Contain("`ARCH001` | `ARCH_DEP_001` | Direct replacement");
            result.Content.Should().Contain("`ARCH022` | `ARCH_OPER_002`, `ARCH_OPER_011`, `ARCH_OPER_012`");
            (await File.ReadAllTextAsync(sourcePath, cancellationToken)).Should().Be(source);
            (await File.ReadAllTextAsync(editorConfigPath, cancellationToken)).Should().Be(editorConfig);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}