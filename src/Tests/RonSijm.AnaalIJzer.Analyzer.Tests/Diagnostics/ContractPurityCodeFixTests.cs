using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class ContractPurityCodeFixTests
{
	[Fact]
	public async Task DisallowedPropertyAccessor_RemovesSetter()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			    <ContractPolicy
			      allowedTypeKinds="Interface"
			      allowedMemberKinds="Property"
			      allowedPropertyAccessors="Get" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public interface IOrderContract
			{
				string Name { get; set; }
			}
			""";

		var newSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ContractPurityViolation,
			"Remove disallowed set accessor");

		newSource.Should().Contain("string Name { get; }");
		newSource.Should().NotContain("set;");
	}

	[Fact]
	public async Task MethodBodyViolation_DoesNotOfferAccessorRemovalFix()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			    <ContractPolicy
			      allowedTypeKinds="Interface"
			      allowedMemberKinds="Method"
			      allowMethodBodies="false" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public interface IOrderContract
			{
				public void Run()
				{
				}
			}
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.ContractPurityViolation);

		titles.Should().BeEmpty();
	}
}
