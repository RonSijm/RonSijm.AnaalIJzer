using RonSijm.IJzer.Analyzer.Tests.Testing;
using RonSijm.IJzer.Core.Findings;

namespace RonSijm.IJzer.Analyzer.Tests.Diagnostics;

public sealed class ArchitecturalInternalCodeFixTests
{
    [Fact]
    public void ArchitecturalInternalBoundary_IsNotListedAsFixable()
    {
        new ArchitecturalLevelCodeFixProvider()
            .FixableDiagnosticIds
            .Should()
            .NotContain(ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task ArchitecturalInternalBoundary_OffersNoSpeculativeCodeFix()
    {
        const string source = """
            namespace RonSijm.IJzer.Annotations
            {
                [System.AttributeUsage(System.AttributeTargets.All)]
                public sealed class ClassInternalAttribute : System.Attribute { }
            }

            [RonSijm.IJzer.Annotations.ClassInternal]
            public sealed class ReflectionProjection { }

            internal sealed class ProjectionConsumer
            {
                private void Receive(ReflectionProjection projection) { }
            }
            """;

        var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);

        titles.Should().BeEmpty();
    }
}
