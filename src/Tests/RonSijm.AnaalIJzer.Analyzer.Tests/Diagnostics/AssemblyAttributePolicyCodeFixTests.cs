using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class AssemblyAttributePolicyCodeFixTests
{
	[Fact]
	public void AssemblyAttributePolicyViolation_IsNotListedAsFixable()
	{
		new ArchitecturalLevelCodeFixProvider()
			.FixableDiagnosticIds
			.Should()
			.NotContain(ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed);
	}

	[Fact]
	public async Task AssemblyAttributePolicyViolation_OffersNoSpeculativeCodeFix()
	{
		const string source = """
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			""";
		const string config = """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy>
			    <Forbidden>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			        <Argument index="0" exactName="NotAllowedExample" />
			      </Attribute>
			    </Forbidden>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed);

		titles.Should().BeEmpty();
	}
}
