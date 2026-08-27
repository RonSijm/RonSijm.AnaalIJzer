using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class InheritancePolicyCodeFixTests
{
	[Fact]
	public async Task MissingRequiredBaseType_AddsBaseType()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Demo.Persistence" />
			    <InheritancePolicy typeKinds="Class" requiredBaseTypes="Entity" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			namespace Demo.Framework
			{
				public abstract class Entity { }
			}

			namespace Demo.Persistence
			{
				public class SyrupEntity { }
			}
			""";

		var newSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.InheritancePolicyViolation,
			"Add required base type 'Entity'");

		newSource.Should().Contain("public class SyrupEntity : Entity");
	}

	[Fact]
	public async Task MissingRequiredInterface_AddsInterface()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Class endsWith="Request" />
			    <InheritancePolicy typeKinds="Class" requiredInterfaces="IPizzaProvider" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public interface IPizzaProvider { }
			public class GetPizzaRequest { }
			""";

		var newSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.InheritancePolicyViolation,
			"Add required interface 'IPizzaProvider'");

		newSource.Should().Contain("public class GetPizzaRequest : IPizzaProvider");
	}

	[Fact]
	public async Task MultipleMissingInterfaces_DoNotOfferAmbiguousFix()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Class endsWith="Request" />
			    <InheritancePolicy typeKinds="Class" requiredInterfaces="IPizzaProvider, ICustomerProvider" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public interface IPizzaProvider { }
			public interface ICustomerProvider { }
			public class GetPizzaRequest { }
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.InheritancePolicyViolation);

		titles.Should().BeEmpty();
	}
}
