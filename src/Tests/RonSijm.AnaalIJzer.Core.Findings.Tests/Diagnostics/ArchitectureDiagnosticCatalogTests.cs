using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

namespace RonSijm.AnaalIJzer.Core.Findings.Tests.Diagnostics;

public sealed class ArchitectureDiagnosticCatalogTests
{
    private static readonly Regex DiagnosticIdPattern = new("^ARCH_[A-Z][A-Z0-9]*_[0-9]{3}$", RegexOptions.CultureInvariant);

    [Fact]
    public void AllDefinitions_HaveValidUniqueIds()
    {
        var definitions = ArchitectureDiagnosticCatalog.All;

        definitions.Should().OnlyContain(definition => DiagnosticIdPattern.IsMatch(definition.Id));
        definitions.Should().OnlyContain(definition => SyntaxFacts.IsValidIdentifier(definition.Id));
        definitions.Select(definition => definition.Id).Should().OnlyHaveUniqueItems();
        definitions.Should().NotContain(definition => definition.Id.EndsWith("_000", StringComparison.Ordinal));
    }

    [Fact]
    public void AllDefinitions_HaveUniqueConcernAndReasonPairs()
    {
        var duplicatePairs = ArchitectureDiagnosticCatalog.All
            .GroupBy(definition => (definition.Concern, definition.Reason))
            .Where(group => group.Count() > 1)
            .ToArray();

        duplicatePairs.Should().BeEmpty();
    }

    [Fact]
    public void AllDefinitions_HaveCompletePresentationMetadata()
    {
        var definitions = ArchitectureDiagnosticCatalog.All;

        definitions.Should().OnlyContain(definition => !string.IsNullOrWhiteSpace(definition.Title));
        definitions.Should().OnlyContain(definition => !string.IsNullOrWhiteSpace(definition.DocumentationPath));
        definitions.Should().OnlyContain(definition => definition.Category == $"Architecture.{definition.Concern}");
    }

    [Fact]
    public void WorkspaceOnlySolutionFindings_AreNotCompilerDiagnostics()
    {
        ArchitectureDiagnosticCatalog.SolutionReferenceNotAllowed.Surface.Should().Be(ArchitectureDiagnosticSurface.Workspace);
        ArchitectureDiagnosticCatalog.SolutionCycle.Surface.Should().Be(ArchitectureDiagnosticSurface.Workspace);
    }

    [Theory]
    [InlineData(ArchitecturalDiagnosticIds.ConfigurationInvalid)]
    [InlineData(ArchitecturalDiagnosticIds.DependencyNotAllowed)]
    [InlineData(ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed)]
    [InlineData(ArchitecturalDiagnosticIds.SolutionReferenceNotAllowed)]
    public void RepresentativeIds_AreLegalCSharpIdentifiers(string diagnosticId)
    {
        SyntaxFacts.IsValidIdentifier(diagnosticId).Should().BeTrue();
    }

    [Fact]
    public void Finding_ExposesCatalogConcernAndReason()
    {
        var finding = new ArchitectureFinding(ArchitectureFindingSeverity.Error, ArchitecturalDiagnosticIds.DependencyNotAllowed, "message", "context");

        finding.Concern.Should().Be(ArchitectureDiagnosticConcern.Dependency);
        finding.Reason.Should().Be(ArchitectureDiagnosticReason.NotAllowed);
        finding.Category.Should().Be("Architecture.Dependency");
        finding.Properties[ArchitectureDiagnosticProperties.PropertyDiagnosticConcern].Should().Be(nameof(ArchitectureDiagnosticConcern.Dependency));
        finding.Properties[ArchitectureDiagnosticProperties.PropertyDiagnosticReason].Should().Be(nameof(ArchitectureDiagnosticReason.NotAllowed));
    }
}