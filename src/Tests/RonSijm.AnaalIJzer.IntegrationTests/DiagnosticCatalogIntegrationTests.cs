using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Findings.Diagnostics;
using RonSijm.AnaalIJzer.Engine;
using RonSijm.AnaalIJzer.IntegrationTests.Support;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests;

public sealed class DiagnosticCatalogIntegrationTests
{
    [Fact]
    public void CompilerDiagnostics_AndDiagnosticPages_HaveAOneToOneMapping()
    {
        var repository = ExampleRepositoryContext.Discover();
        var analyzerIds = new ArchitecturalLevelAnalyzer().SupportedDiagnostics
            .Select(descriptor => descriptor.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var definitions = analyzerIds.Select(ArchitectureDiagnosticCatalog.Get).ToArray();
        var documentedPaths = definitions
            .Select(definition => Path.GetFullPath(Path.Combine(repository.RepositoryRoot, "docs", definition.DocumentationPath.Replace('/', Path.DirectorySeparatorChar) + ".md")))
            .ToArray();

        documentedPaths.Should().OnlyContain(path => File.Exists(path));
        documentedPaths.Should().OnlyHaveUniqueItems();

        var diagnosticPages = Directory
            .EnumerateFiles(Path.Combine(repository.RepositoryRoot, "docs", "diagnostics"), "arch_*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        documentedPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Should().Equal(diagnosticPages);
    }
}