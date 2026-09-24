using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class NameRuleLanguageFormsTests
{
    private const string Config = """
		<ArchitecturalLevels>
		  <Layer name="Application">
		    <Class endsWith="Service" />
		    <NameRules>
		      <RequireMatchingNames>
		        <Name endsWith="Id" />
		      </RequireMatchingNames>
		    </NameRules>
		  </Layer>
		</ArchitecturalLevels>
		""";

    [Fact]
    public async Task CompoundAssignment_ReportsValueMovement()
    {
        const string source = """
			class OrderService
			{
				void Run(int animalId)
				{
					var fruitId = 0;
					fruitId += animalId;
				}
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, Config);

        var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Which;
        diagnostic.Properties["Site"].Should().Be("Local");
        diagnostic.Properties["SourceName"].Should().Be("animalId");
        diagnostic.Properties["TargetName"].Should().Be("fruitId");
    }

    [Fact]
    public async Task DeconstructionAssignment_ReportsEveryMismatchedComponent()
    {
        const string source = """
			class OrderService
			{
				void Run()
				{
					var fruitId = 1;
					var animalId = 2;
					(fruitId, animalId) = (animalId, fruitId);
				}
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, Config);

        var nameDiagnostics = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).ToArray();
        nameDiagnostics.Should().HaveCount(2);
        nameDiagnostics.Select(item => item.Properties["SourceName"]).Should().BeEquivalentTo("animalId", "fruitId");
        nameDiagnostics.Select(item => item.Properties["TargetName"]).Should().BeEquivalentTo("fruitId", "animalId");
    }

    [Fact]
    public async Task ConditionalAndWrapperExpressions_ReportOnlyTheMismatchedSourceBranch()
    {
        const string source = """
			class OrderService
			{
				string? animalId = "animal";
				string? fruitId = "fruit";

				void Run(bool useAnimal)
				{
					fruitId = useAnimal ? (animalId as string)! : fruitId;
				}
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, Config);

        var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Which;
        diagnostic.Properties["SourceName"].Should().Be("animalId");
        diagnostic.Properties["TargetName"].Should().Be("fruitId");
    }

    [Fact]
    public async Task ExpressionBodiedMembersAndLocalFunctions_UseTheirOwnReturnTargets()
    {
        const string source = """
			using System;

			class OrderService
			{
				private readonly int animalId = 7;
				public int FruitId => animalId;
				public int GetFruitId() => animalId;

				public int GetOrderId()
				{
					Func<int> anonymous = () => animalId;
					int GetAnimalId()
					{
						return animalId;
					}

					return animalId;
				}
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, Config);

        var nameDiagnostics = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).ToArray();
        nameDiagnostics.Should().HaveCount(3);
        nameDiagnostics.Select(item => item.Properties["Site"]).Should().BeEquivalentTo("Property", "MethodReturn", "MethodReturn");
        nameDiagnostics.Select(item => item.Properties["TargetName"]).Should().BeEquivalentTo("FruitId", "GetFruitId", "GetOrderId");
    }

    [Fact]
    public async Task NamedInAndOutArguments_RespectValueDirection()
    {
        const string source = """
			class OrderService
			{
				void Run(int fruitId, int animalId)
				{
					Save(animalId: fruitId, fruitId: animalId);
					Read(in animalId);
					Create(out animalId);
				}

				void Save(int fruitId, int animalId = 0) { }
				void Read(in int fruitId) { }
				void Create(out int fruitId)
				{
					fruitId = 0;
				}
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, Config);

        var nameDiagnostics = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).ToArray();
        nameDiagnostics.Should().HaveCount(4);
        nameDiagnostics.Should().OnlyContain(item => item.Properties["Site"] == "Method");
        nameDiagnostics.Select(item => item.Properties["SourceName"]).Should().Contain("fruitId", "animalId");
        nameDiagnostics.Select(item => item.Properties["TargetName"]).Should().Contain("fruitId", "animalId");
    }
}