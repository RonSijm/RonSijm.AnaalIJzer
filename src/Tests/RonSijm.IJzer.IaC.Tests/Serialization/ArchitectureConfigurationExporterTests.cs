using System.Text;
using System.Xml.Linq;
using RonSijm.IJzer.Core.Configuration.Document.Validation;

namespace RonSijm.IJzer.IaC.Tests.Serialization;

public sealed class ArchitectureConfigurationExporterTests
{
    [Fact]
    public void Export_WritesDeterministicReadableAnl()
    {
        var configuration = new ArchitectureConfiguration
        {
            Description = "The restaurant dependency rules.",
            Elements =
            {
                new Layer("Waiter")
                {
                    Elements = { new ClassMatcher { EndsWith = "Waiter" } }
                },
                new Layer("Chef")
                {
                    Elements = { new ClassMatcher { EndsWith = "Chef" } }
                },
                new AllowedDependency("Waiter", "Chef")
            }
        };

        var result = configuration.ToXml();

        result.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        result.Should().Contain("<Layer name=\"Waiter\">");
        result.Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Chef\" />");
        configuration.ToXml().Should().Be(result);
    }

    [Fact]
    public void Save_WritesUtf8WithoutByteOrderMarkAndOptionalSchemaHint()
    {
        var directory = Path.Combine(Path.GetTempPath(), "IJzer-IaC-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "Architecture.anl");
        try
        {
            var configuration = new ArchitectureConfiguration();
            configuration.Save(path, new ArchitectureConfigurationExportOptions { SchemaLocation = "IJzer.xsd" });

            var bytes = File.ReadAllBytes(path);
            var text = Encoding.UTF8.GetString(bytes);
            bytes.Take(3).Should().NotEqual([0xEF, 0xBB, 0xBF]);
            text.Should().Contain("xsi:noNamespaceSchemaLocation=\"IJzer.xsd\"");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void Export_AllFeatureFamiliesValidateAgainstTheRealSchema()
    {
        var configuration = AllFeaturesConfigurationFactory.Create();
        var document = XDocument.Parse(configuration.ToXml(), LoadOptions.SetLineInfo);

        var issues = ArchitectureConfigurationValidator.Validate(document, "Generated.anl");

        issues.Should().BeEmpty(string.Join(Environment.NewLine, issues.Select(issue => issue.Message)));
    }

    [Fact]
    public void Export_AllDeclaredSchemaElementNamesAreRepresentedByTheIaCFixture()
    {
        var configuration = AllFeaturesConfigurationFactory.Create();
        var emittedNames = XDocument.Parse(configuration.ToXml()).Root!.DescendantsAndSelf().Select(element => element.Name.LocalName).ToHashSet(StringComparer.Ordinal);
        var schema = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Scheme", "IJzer.xsd"));
        XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        var schemaNames = schema.Descendants(xs + "element")
            .Select(element => (string?)element.Attribute("name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

        emittedNames.Should().Contain(schemaNames);
    }

    [Fact]
    public void Export_RejectsAllowedAndBlockedSitesOnTheSameElement()
    {
        var dependency = new AllowedDependency("Waiter", "Chef");
        dependency.AllowedSites.Add(DependencySite.Constructor);
        dependency.BlockedSites.Add(DependencySite.Field);
        var configuration = new ArchitectureConfiguration { Elements = { dependency } };

        Action action = () => configuration.ToXml();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot define both allowed sites and blocked sites*");
    }

}
