using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

public sealed class ExampleSettingsValidationTests
{
    [Fact]
    public void ExampleSettingsConfigs_ResolveSchemaHintsWithExactFileSystemCasing()
    {
        var context = ExampleRepositoryContext.Discover();
        var failures = new List<string>();
        ExampleSettingsValidation.ValidateExampleSettingsConfigs(context, failures);

        failures.Should().BeEmpty("every example schema hint should resolve with exact filesystem casing:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void SchemaHintExists_RequiresTheExactFileNameCasing()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-schema-hint-casing-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var settingsPath = Path.Combine(temporaryDirectory, "Architecture.anl");
            var schemaPath = Path.Combine(temporaryDirectory, "AnaalIJzer.xsd");
            File.WriteAllText(settingsPath, "<ArchitecturalLevels />");
            File.WriteAllText(schemaPath, "<schema />");

            ExampleSettingsValidation.SchemaHintExists(settingsPath, "AnaalIJzer.xsd").Should().BeTrue();
            ExampleSettingsValidation.SchemaHintExists(settingsPath, "AnaalIjzer.xsd").Should().BeFalse();
        }
        finally
        {
            Directory.Delete(temporaryDirectory, true);
        }
    }
}