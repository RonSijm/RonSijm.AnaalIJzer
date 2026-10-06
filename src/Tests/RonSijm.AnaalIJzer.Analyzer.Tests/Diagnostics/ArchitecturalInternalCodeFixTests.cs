using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

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
            namespace RonSijm.AnaalIJzer.Annotations
            {
                [System.AttributeUsage(System.AttributeTargets.All)]
                public sealed class ClassInternalAttribute : System.Attribute { }
            }

            [RonSijm.AnaalIJzer.Annotations.ClassInternal]
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
