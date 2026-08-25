using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Contracts;

public sealed class ContractPurityAnalyzerTests
{
	[Fact]
	public async Task ContractPolicy_RejectsDefaultInterfaceMethodBody()
	{
		const string source = """
			public interface IOrderContract
			{
				public void Run()
				{
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			    <ContractPolicy
			      allowedTypeKinds="Interface"
			      allowedMemberKinds="Method"
			      allowMethodBodies="false"
			      description="Contracts stay abstract." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ContractPurityViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("Run");
		violation.Properties[ArchitecturalDiagnostics.PropertyContractViolationKind].Should().Be("MethodBodyForbidden");
		violation.GetMessage().Should().Contain("allowMethodBodies='false'");
	}

	[Fact]
	public async Task ContractPolicy_RejectsDisallowedPropertyAccessor()
	{
		const string source = """
			public interface IOrderContract
			{
				string Name { get; set; }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			    <ContractPolicy
			      allowedTypeKinds="Interface"
			      allowedMemberKinds="Property"
			      allowedPropertyAccessors="Get"
			      description="Contracts expose getters only." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ContractPurityViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("Name");
		violation.Properties[ArchitecturalDiagnostics.PropertyContractViolationKind].Should().Be("DisallowedPropertyAccessor");
		violation.GetMessage().Should().Contain("allows only property accessors Get");
	}

	[Fact]
	public async Task ParentAndChildContractPolicies_AreCumulativeAndOuterFailureWins()
	{
		const string source = """
			namespace CandyShop.Contracts;

			public interface IOrderContract
			{
				string Name { get; }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Assembly exactName="TestAssembly" />
			    <ContractPolicy
			      allowedTypeKinds="Interface"
			      allowedMemberKinds="Method"
			      description="Outer contract boundary." />
			    <Layer name="Contracts">
			      <Namespace startsWith="CandyShop.Contracts" />
			      <ContractPolicy
			        allowedTypeKinds="Interface"
			        allowedMemberKinds="Property"
			        description="Child contract boundary." />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ContractPurityViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyCallerLayerName].Should().Be("Application/Contracts");
		violation.GetMessage().Should().Contain("layer 'Application'");
		violation.GetMessage().Should().Contain("allows only member kinds Method");
	}

	[Theory]
	[InlineData("""<ContractPolicy allowedTypeKinds="" allowedMemberKinds="Method" />""")]
	[InlineData("""<ContractPolicy allowedTypeKinds="Interface" allowedMemberKinds="" />""")]
	[InlineData("""<ContractPolicy allowedTypeKinds="Unknown" allowedMemberKinds="Method" />""")]
	[InlineData("""<ContractPolicy allowedTypeKinds="Interface" allowedMemberKinds="Unknown" />""")]
	[InlineData("""<ContractPolicy allowedTypeKinds="Interface" allowedMemberKinds="Method" allowedPropertyAccessors="Unknown" />""")]
	[InlineData("""<ContractPolicy allowedTypeKinds="Interface" allowedMemberKinds="Method" allowMethodBodies="maybe" />""")]
	public async Task InvalidPolicies_ReportConfigurationIssue(string policy)
	{
		var config = $"""
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			    {policy}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public interface IOrderContract { void Run(); }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ContractPurityViolation);
	}

	[Fact]
	public async Task ConfigurationWithoutContractPolicy_RemainsUnchanged()
	{
		const string source = """
			public interface IOrderContract
			{
				public void Run()
				{
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ContractPurityViolation);
	}
}
