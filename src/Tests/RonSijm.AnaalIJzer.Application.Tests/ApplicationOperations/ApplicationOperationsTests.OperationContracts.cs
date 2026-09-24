using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Application.OperationContracts;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
    [Fact]
    public void OperationContracts_ReportMissingAndAmbiguousOwnersAcrossInspectedProjects()
    {
        var config = ArchitecturalConfigParser.Parse(
            [
                new OperationContractAdditionalText(
                    "Architecture.anl",
                    """
					<ArchitecturalLevels>
					  <Operations>
					    <Operation name="PlacePizzaOrder">
					      <Owner>
					        <DeclarationMatcher>
					          <ContainingType endsWith="Kitchen" />
					          <Member exactName="PlacePizzaOrder" memberKind="Method" />
					        </DeclarationMatcher>
					      </Owner>
					    </Operation>
					  </Operations>
					</ArchitecturalLevels>
					""")
            ],
            CancellationToken.None);
        var missingOwnerProject = CreateOperationProject(
            config,
            "NoKitchen",
            "public sealed class PizzaOrderController { }");
        var ambiguousOwnerProject = CreateOperationProject(
            config,
            "TwoKitchens",
            """
			public sealed class PizzaKitchen { public void PlacePizzaOrder() { } }
			public sealed class EveningKitchen { public void PlacePizzaOrder() { } }
			""");

        var missingFindings = OperationContractInspectionService.GetFindings([missingOwnerProject], CancellationToken.None);
        var ambiguousFindings = OperationContractInspectionService.GetFindings([ambiguousOwnerProject], CancellationToken.None);

        missingFindings.Should().ContainSingle(finding => finding.Code == ArchitectureFindingCodes.OperationContractOwnerMissing);
        ambiguousFindings.Should().ContainSingle(finding => finding.Code == ArchitectureFindingCodes.OperationContractOwnerAmbiguous);
        ambiguousFindings[0].Context.Should().Contain("PizzaKitchen.PlacePizzaOrder()");
        ambiguousFindings[0].Context.Should().Contain("EveningKitchen.PlacePizzaOrder()");
    }

    [Fact]
    public void OperationContracts_FindOwnersAcrossTheInspectedSolution()
    {
        var config = ArchitecturalConfigParser.Parse(
            [
                new OperationContractAdditionalText(
                    "Architecture.anl",
                    """
					<ArchitecturalLevels>
					  <Operations>
					    <Operation name="PlacePizzaOrder">
					      <Owner>
					        <DeclarationMatcher>
					          <ContainingType endsWith="Kitchen" />
					          <Member exactName="PlacePizzaOrder" memberKind="Method" />
					        </DeclarationMatcher>
					      </Owner>
					    </Operation>
					  </Operations>
					</ArchitecturalLevels>
					""")
            ],
            CancellationToken.None);
        var configuredProject = CreateOperationProject(config, "Ordering.Contracts", "public sealed class PizzaOrderRequest { }");
        var ownerProject = CreateOperationProject(
            RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig.Empty,
            "Ordering.Application",
            "public sealed class PizzaKitchen { public void PlacePizzaOrder() { } }");

        var findings = OperationContractInspectionService.GetFindings([configuredProject, ownerProject], CancellationToken.None);

        findings.Should().BeEmpty();
    }

    private static ProjectAnalysisResult CreateOperationProject(
        RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig config,
        string assemblyName,
        string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var projectDirectory = Path.Combine(Path.GetTempPath(), "AnaalIJzer-OperationContract-" + assemblyName);
        var result = new ProjectAnalysisResult(
            Path.Combine(projectDirectory, assemblyName + ".csproj"),
            projectDirectory,
            assemblyName,
            compilation,
            config,
            null,
            null,
            "Architecture.anl",
            null,
            ImmutableArray<Diagnostic>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<string>.Empty);

        return result;
    }

    private sealed class OperationContractAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            var result = _text;

            return result;
        }
    }
}