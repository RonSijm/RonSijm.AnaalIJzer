using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class NameRuleIntraProceduralTrackingTests
{
	[Fact]
	public async Task DirectTracking_DoesNotFollowAnOpaqueLocalAlias()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CreateSource("""
			var alias = animalId;
			Save(alias);
			"""), CreateConfig("Direct"));

		diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Should().BeEmpty();
	}

	[Fact]
	public async Task IntraProceduralTracking_FollowsAnUnambiguousLocalAliasToAnInvocation()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CreateSource("""
			var alias = animalId;
			Save(alias);
			"""), CreateConfig("IntraProcedural"));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Which;
		diagnostic.Properties["SourceName"].Should().Be("animalId");
		diagnostic.Properties["TargetName"].Should().Be("fruitId");
		diagnostic.Properties["Site"].Should().Be("Method");
	}

	[Fact]
	public async Task IntraProceduralTracking_FollowsAnUnambiguousLocalAliasToAReturn()
	{
		const string source = """
			class OrderService
			{
				int GetFruitId(int animalId)
				{
					var alias = animalId;
					return alias;
				}
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig("IntraProcedural"));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Which;
		diagnostic.Properties["SourceName"].Should().Be("animalId");
		diagnostic.Properties["TargetName"].Should().Be("GetFruitId");
		diagnostic.Properties["Site"].Should().Be("MethodReturn");
	}

	[Fact]
	public async Task IntraProceduralTracking_DropsAmbiguousBranchProvenance()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CreateSource("""
			var alias = useAnimal ? animalId : fruitId;
			Save(alias);
			"""), CreateConfig("IntraProcedural"));

		diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Should().BeEmpty();
	}

	[Fact]
	public async Task IntraProceduralTracking_FollowsAnUnambiguousLocalAliasInsideALambda()
	{
		const string source = """
			using System;

			class OrderService
			{
				void Run(int animalId)
				{
					Action save = () =>
					{
						var alias = animalId;
						Save(alias);
					};

					save();
				}

				void Save(int fruitId) { }
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig("IntraProcedural"));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NameShapeMismatch).Which;
		diagnostic.Properties["SourceName"].Should().Be("animalId");
		diagnostic.Properties["TargetName"].Should().Be("fruitId");
		diagnostic.Properties["Site"].Should().Be("Method");
	}

	private static string CreateConfig(string valueTracking)
	{
		var result = $$"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames valueTracking="{{valueTracking}}">
			        <Source endsWith="Id" />
			        <Target endsWith="Id" />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";

		return result;
	}

	private static string CreateSource(string body)
	{
		var result = $$"""
			class OrderService
			{
				void Run(int animalId, int fruitId, bool useAnimal)
				{
			{{body}}
				}

				void Save(int fruitId) { }
			}
			""";

		return result;
	}
}
