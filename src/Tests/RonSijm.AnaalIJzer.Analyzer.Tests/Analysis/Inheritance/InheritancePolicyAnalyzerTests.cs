using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Inheritance;

public sealed class InheritancePolicyAnalyzerTests
{
	[Fact]
	public async Task InheritancePolicy_RejectsMissingRequiredBaseType()
	{
		const string source = """
			namespace Demo.Framework
			{
				public abstract class Entity { }
			}

			namespace Demo.Persistence
			{
				public class CandyEntity : Demo.Framework.Entity { }
				public class SyrupEntity { }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Demo.Persistence" />
			    <InheritancePolicy
			      typeKinds="Class"
			      requiredBaseTypes="Entity"
			      description="Persistence entities inherit Entity." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("SyrupEntity");
		violation.Properties[ArchitecturalDiagnostics.PropertyInheritanceViolationKind].Should().Be("MissingRequiredBaseType");
		violation.GetMessage().Should().Contain("requires a base type matching Entity");
	}

	[Fact]
	public async Task ParentAndChildInheritancePolicies_AreCumulativeAndOuterFailureWins()
	{
		const string source = """
			namespace Demo.Framework
			{
				public abstract class Entity { }
			}

			namespace Demo.Persistence.Specialized
			{
				public class CandyEntity { }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Persistence">
			    <Namespace startsWith="Demo.Persistence" />
			    <InheritancePolicy
			      typeKinds="Class"
			      requiredBaseTypes="Entity"
			      description="All persistence entities inherit Entity." />
			    <Layer name="Specialized">
			      <Namespace startsWith="Demo.Persistence.Specialized" />
			      <InheritancePolicy
			        typeKinds="Class"
			        requiredInterfaces="IAuditedEntity"
			        description="Specialized persistence entities are audited." />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyCallerLayerName].Should().Be("Persistence/Specialized");
		violation.GetMessage().Should().Contain("layer 'Persistence'");
		violation.GetMessage().Should().Contain("requires a base type matching Entity");
	}

	[Theory]
	[InlineData("""<InheritancePolicy requiredBaseTypes="Entity" />""")]
	[InlineData("""<InheritancePolicy typeKinds="Class" />""")]
	[InlineData("""<InheritancePolicy typeKinds="Unknown" requiredBaseTypes="Entity" />""")]
	public async Task InvalidPolicies_ReportConfigurationIssue(string policy)
	{
		var config = $"""
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Demo.Persistence" />
			    {policy}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("namespace Demo.Persistence; public class CandyEntity { }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation);
	}

	[Fact]
	public async Task ConfigurationWithoutInheritancePolicy_RemainsUnchanged()
	{
		const string source = """
			namespace Demo.Persistence;
			public class CandyEntity { }
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Demo.Persistence" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation);
	}
}
