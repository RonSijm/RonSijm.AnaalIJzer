using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Engine;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Visibility;

public sealed class ArchitecturalInternalAnalyzerTests
{
    private const string AnnotationDeclarations = """
        namespace RonSijm.AnaalIJzer.Annotations
        {
            [System.AttributeUsage(System.AttributeTargets.All)]
            public sealed class ClassInternalAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.All)]
            public sealed class AssemblyInternalAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.All)]
            public sealed class LayerInternalAttribute : System.Attribute { }
        }
        """;

    [Fact]
    public async Task ClassInternalMember_AllowsDeclaringTypeAndRejectsAnotherType()
    {
        var source = AnnotationDeclarations + """

            public sealed class ProjectionBuilder
            {
                [RonSijm.AnaalIJzer.Annotations.ClassInternal]
                public void Reset() { }

                public void ResetItself() => Reset();
            }

            internal sealed class ProjectionConsumer
            {
                private void Reset(ProjectionBuilder builder) => builder.Reset();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);
        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary).Subject;
        violation.Properties[ArchitectureDiagnosticProperties.PropertyArchitecturalInternalScope].Should().Be("Class");
        violation.Properties[ArchitectureDiagnosticProperties.PropertyArchitecturalInternalAttribute].Should().EndWith("ClassInternalAttribute");
    }

    [Fact]
    public async Task ClassInternalType_AllowsItsOwnFactoryAndRejectsExternalConstruction()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.ClassInternal]
            public sealed class ReflectionProjection
            {
                public static ReflectionProjection Create() => new();
            }

            internal sealed class ProjectionConsumer
            {
                private ReflectionProjection Create() => new ReflectionProjection();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task AssemblyInternalType_AllowsPrivateUseButRejectsPublicApiExposure()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.AssemblyInternal]
            public sealed class EntityProjection { }

            public sealed class ProjectionRepository
            {
                private EntityProjection Build() => new();

                public EntityProjection Leak() => Build();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);
        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary).Subject;
        violation.GetMessage().Should().Contain("externally visible declaration");
    }

    [Fact]
    public async Task AssemblyInternalType_RejectsReferenceFromAnotherAssembly()
    {
        var producer = CreateMetadataReference("ProjectionAssembly", AnnotationDeclarations + """

            namespace Persistence
            {
                [RonSijm.AnaalIJzer.Annotations.AssemblyInternal]
                public sealed class EntityProjection { }
            }
            """);
        var consumerSource = """
            namespace Application;

            internal sealed class ProjectionConsumer
            {
                private Persistence.EntityProjection? projection;
            }
            """;

        var diagnostics = await GetDiagnosticsAsync("ConsumerAssembly", consumerSource, producer);
        diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task LayerInternalType_AllowsOwningLayerAndRejectsAnotherLayer()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.LayerInternal]
            public sealed class EntityProjection { }

            internal sealed class ProjectionRepository
            {
                private EntityProjection Build() => new();
            }

            internal sealed class OrderingService
            {
                private EntityProjection? projection;
            }
            """;
        var config = """
            <ArchitecturalLevels>
              <Layer name="Persistence">
                <Class typeName="EntityProjection" />
                <Class typeName="ProjectionRepository" />
              </Layer>
              <Layer name="Application">
                <Class typeName="OrderingService" />
              </Layer>
              <AllowedDependency from="Persistence" to="Persistence" />
            </ArchitecturalLevels>
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary).Subject;
        violation.Properties[ArchitectureDiagnosticProperties.PropertyCallerLayerName].Should().Be("Application");
        violation.Properties[ArchitectureDiagnosticProperties.PropertyDepLayerName].Should().Be("Persistence");
    }

    [Fact]
    public async Task LayerInternalType_AllowsDescendantLayer()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.LayerInternal]
            public sealed class EntityProjection { }

            internal sealed class ProjectionMapper
            {
                private EntityProjection Build() => new();
            }
            """;
        var config = """
            <ArchitecturalLevels>
              <Layer name="Persistence">
                <Class typeName="EntityProjection" />
                <Class typeName="ProjectionMapper" />
                <Layer name="Mapping">
                  <Class typeName="ProjectionMapper" />
                </Layer>
                <AllowedDependency from="Mapping" to="/Persistence" />
              </Layer>
            </ArchitecturalLevels>
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task LayerInternalType_WithoutLayerConfigurationFailsClosed()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.LayerInternal]
            public sealed class EntityProjection { }

            internal sealed class ProjectionConsumer
            {
                private EntityProjection? projection;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);
        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary).Subject;
        violation.GetMessage().Should().Contain("requires 'EntityProjection' to belong to a configured layer");
    }

    [Fact]
    public async Task ClassInternalType_AllowsNestedTypeFamilyAndProtectedPublicSignature()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.ClassInternal]
            public sealed class ReflectionProjection
            {
                public sealed class Builder
                {
                    public ReflectionProjection Build() => new();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task AssemblyInternalType_AllowsSignatureProtectedByAssemblyInternalOwner()
    {
        var source = AnnotationDeclarations + """

            [RonSijm.AnaalIJzer.Annotations.AssemblyInternal]
            public sealed class EntityProjection { }

            [RonSijm.AnaalIJzer.Annotations.AssemblyInternal]
            public sealed class ProjectionFacade
            {
                public EntityProjection Build() => new();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task ClassInternalOperator_RejectsUseFromAnotherType()
    {
        var source = AnnotationDeclarations + """

            public readonly struct Portion
            {
                [RonSijm.AnaalIJzer.Annotations.ClassInternal]
                public static Portion operator +(Portion left, Portion right) => left;
            }

            internal sealed class PortionCalculator
            {
                public Portion Add(Portion left, Portion right) => left + right;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    [Fact]
    public async Task CompilationWithoutArchitecturalInternalAnnotations_RemainsSilent()
    {
        const string source = """
            public sealed class Projection { }

            internal sealed class ProjectionConsumer
            {
                private void Receive(Projection projection) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ArchitecturalInternalBoundary);
    }

    private static MetadataReference CreateMetadataReference(string assemblyName, string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(SourceText.From(source))],
            GetBasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitResult.Diagnostics));
        }

        var result = MetadataReference.CreateFromImage(stream.ToArray());

        return result;
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string assemblyName, string source, MetadataReference reference)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(SourceText.From(source), path: "Consumer.cs")],
            [.. GetBasicReferences(), reference],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var compilationWithAnalyzers = compilation.WithAnalyzers([new ArchitecturalLevelAnalyzer()]);
        var result = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return result;
    }

    private static MetadataReference[] GetBasicReferences()
    {
        var result = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        return result;
    }
}
