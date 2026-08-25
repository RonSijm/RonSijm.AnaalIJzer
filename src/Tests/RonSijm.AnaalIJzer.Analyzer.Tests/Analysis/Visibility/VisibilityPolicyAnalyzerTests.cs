using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Visibility;

public sealed class VisibilityPolicyAnalyzerTests
{
	[Fact]
	public async Task TypeAllowList_RejectsPublicAndAllowsInternalAndFileTypes()
	{
		const string source = """
			public class PublicQueryable { }
			internal class InternalQueryable { }
			file class FileQueryable { }
			""";
		var config = CreateConfig("Type", allowed: "Internal, File");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("PublicQueryable");
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclarationTarget].Should().Be("Type");
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredAccessibility].Should().Be("Public");
	}

	[Fact]
	public async Task EveryDeclarationTarget_IsClassified()
	{
		const string source = """
			using System;

			public class PolicySubject
			{
				public PolicySubject() { }
				public void Run() { }
				public int Value { get; set; }
				public int Field;
				public event EventHandler? Changed;
				public static PolicySubject operator +(PolicySubject left, PolicySubject right) => left;
				public static explicit operator int(PolicySubject value) => 0;

				public class Nested { }
			}
			""";
		const string targets = "Type, Constructor, Method, Property, Field, Event, Operator, Conversion, NestedType";
		var config = CreateConfig(targets, blocked: "Public");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation).ToArray();
		violations.Should().HaveCount(9);
		violations.Select(item => item.Properties[ArchitecturalDiagnostics.PropertyDeclarationTarget]).Should().BeEquivalentTo(
			"Type", "Constructor", "Method", "Property", "Field", "Event", "Operator", "Conversion", "NestedType");
	}

	[Fact]
	public async Task DefaultAndInterfaceAccessibilities_UseRoslynDeclaredAccessibility()
	{
		const string source = """
			interface IPolicySubject
			{
				void Run();
			}

			class PolicySubject
			{
				int Value;
			}
			""";
		var config = CreateConfig("Type, Method, Field", blocked: "Internal, Private, Public");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation).ToArray();
		violations.Should().HaveCount(4);
		violations.Select(item => item.Properties[ArchitecturalDiagnostics.PropertyDeclaredAccessibility]).Should().BeEquivalentTo("Internal", "Public", "Internal", "Private");
	}

	[Fact]
	public async Task ExplicitInterfaceImplementation_UsesPrivateAccessibility()
	{
		const string source = """
			interface IPolicySubject
			{
				void Run();
			}

			class PolicySubject : IPolicySubject
			{
				void IPolicySubject.Run() { }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Class typeName="PolicySubject" />
			    <VisibilityPolicy targets="Method" allowedAccessibilities="Private" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation);
	}

	[Fact]
	public async Task ParentAndChildPolicies_AreCumulativeAndOuterFailureWins()
	{
		const string source = """
			public class ChildService
			{
				public void Run() { }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Assembly exactName="TestAssembly" />
			    <VisibilityPolicy targets="Method" blockedAccessibilities="Public" description="Outer policy" />
			    <Layer name="Services">
			      <Class endsWith="Service" />
			      <VisibilityPolicy targets="Method" allowedAccessibilities="Public" description="Child policy" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation).Subject;
		violation.GetMessage().Should().Contain("layer 'Application' blocks Public");
		violation.Properties[ArchitecturalDiagnostics.PropertyCallerLayerName].Should().Be("Application/Services");
	}

	[Fact]
	public async Task MultipleOverlappingPolicies_MustAllPass()
	{
		const string source = """
			public class PolicySubject
			{
				protected void Run() { }
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Assembly exactName="TestAssembly" />
			    <VisibilityPolicy targets="Method" allowedAccessibilities="Public, Protected" />
			    <VisibilityPolicy targets="Method" blockedAccessibilities="Protected" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation);
	}

	[Fact]
	public async Task PartialDeclarations_ReportOnce()
	{
		const string source = """
			public partial class PolicySubject
			{
				public partial void Run();
			}

			public partial class PolicySubject
			{
				public partial void Run() { }
			}
			""";
		var config = CreateConfig("Method", blocked: "Public");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation);
	}

	[Theory]
	[InlineData("""<VisibilityPolicy targets="Method" />""")]
	[InlineData("""<VisibilityPolicy targets="Method" allowedAccessibilities="Public" blockedAccessibilities="Private" />""")]
	[InlineData("""<VisibilityPolicy targets="Unknown" allowedAccessibilities="Public" />""")]
	[InlineData("""<VisibilityPolicy targets="Method" allowedAccessibilities="Unknown" />""")]
	[InlineData("""<VisibilityPolicy targets="" allowedAccessibilities="Public" />""")]
	[InlineData("""<VisibilityPolicy targets="Method" allowedAccessibilities="" />""")]
	public async Task InvalidPolicies_ReportConfigurationIssue(string policy)
	{
		var config = $"""
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Assembly exactName="TestAssembly" />
			    {policy}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class PolicySubject { }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation);
	}

	[Fact]
	public async Task ConfigurationWithoutVisibilityPolicy_RemainsUnchanged()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Assembly exactName="TestAssembly" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class PolicySubject { public void Run() { } }", config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation);
	}

	private static string CreateConfig(string targets, string? allowed = null, string? blocked = null)
	{
		var accessibilityAttribute = allowed is not null
			? $"""allowedAccessibilities="{allowed}" """
			: $"""blockedAccessibilities="{blocked}" """;
		var result = $"""
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Assembly exactName="TestAssembly" />
			    <VisibilityPolicy targets="{targets}" {accessibilityAttribute}/>
			  </Layer>
			</ArchitecturalLevels>
			""";

		return result;
	}
}
