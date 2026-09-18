using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.AssemblyAttributes;

public sealed class AssemblyAttributePolicyAnalyzerTests
{
	[Fact]
	public async Task ForbiddenFriendAssembly_ReportsArch024ForTheRejectedAttributeOnly()
	{
		const string source = """
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("AllowedExample")]
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			
			public sealed class PizzaVault { }
			""";
		const string config = """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy description="Only reviewed projects receive kitchen keys.">
			    <Forbidden>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			        <Argument index="0" exactName="NotAllowedExample" />
			      </Attribute>
			    </Forbidden>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyAssemblyAttributeTypeName].Should().Be("System.Runtime.CompilerServices.InternalsVisibleToAttribute");
		violation.Properties[ArchitecturalDiagnostics.PropertyAssemblyAttributePolicyRule].Should().Contain("NotAllowedExample");
	}

	[Fact]
	public async Task AllowedFriendAssembly_ReportsArch024WhenTheAttributeValueIsNotAllowed()
	{
		const string source = """
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("AllowedExample")]
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			""";
		const string config = """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy>
			    <Allowed>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			        <Argument index="0" exactName="AllowedExample" />
			      </Attribute>
			    </Allowed>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed);
	}

	[Fact]
	public async Task UnrelatedAssemblyAttributes_AreNotSelectedByThePolicy()
	{
		const string source = """
			using System;
			[assembly: CLSCompliant(true)]
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

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed);
	}

	[Fact]
	public async Task InlineAssemblyMetadataSettings_AlsoApplyToAssemblyAttributes()
	{
		const string source = """"
			using System.Reflection;
			using System.Runtime.CompilerServices;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy>
			    <Forbidden>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			        <Argument index="0" exactName="NotAllowedExample" />
			      </Attribute>
			    </Forbidden>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""")]
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			"""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed);
	}
}
