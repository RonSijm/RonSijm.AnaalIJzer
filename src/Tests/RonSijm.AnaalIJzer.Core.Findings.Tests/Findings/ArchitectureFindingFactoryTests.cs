using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RonSijm.AnaalIJzer.Core.Findings.Tests.Findings;

public sealed class ArchitectureFindingFactoryTests
{
	[Fact]
	public void FromDiagnostic_UsesExplicitContext_WhenProvided()
	{
		var diagnostic = CreateDiagnostic(Location.None, ImmutableDictionary<string, string?>.Empty);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic, "custom context");

		result.Context.Should().Be("custom context");
		result.Severity.Should().Be(ArchitectureFindingSeverity.Warning);
	}

	[Fact]
	public void FromDiagnostic_FormatsSourceLocation_WhenContextIsMissing()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tree = CSharpSyntaxTree.ParseText("class PizzaKitchen { }", path: @"D:\repo\Kitchen.cs", cancellationToken: cancellationToken);
		var diagnostic = CreateDiagnostic(tree.GetRoot(cancellationToken).GetLocation(), ImmutableDictionary<string, string?>.Empty);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic);

		result.Context.Should().Be(@"D:\repo\Kitchen.cs:1");
	}

	[Fact]
	public void FromDiagnostic_UsesEmptyContext_ForNonSourceLocations()
	{
		var diagnostic = CreateDiagnostic(Location.None, ImmutableDictionary<string, string?>.Empty);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic);

		result.Context.Should().BeEmpty();
	}

	[Fact]
	public void FromDiagnostic_UsesExceptionStatus_AsState()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertyExceptionStatus, "ExpiringSoon");
		var diagnostic = CreateDiagnostic(Location.None, properties);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic);

		result.State.Should().Be("ExpiringSoon");
	}

	[Fact]
	public void FromDiagnostic_PrefersEntryPointFailureReason_ForReasonCode()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertyEntryPointFailureReason, "WrongEntryPoint")
			.Add(ArchitectureDiagnosticProperties.PropertyContractViolationKind, "Setter")
			.Add(ArchitectureDiagnosticProperties.PropertyNameRuleKind, "VariableNameMismatch");
		var diagnostic = CreateDiagnostic(Location.None, properties);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic);

		result.ReasonCode.Should().Be("WrongEntryPoint");
	}

	[Fact]
	public void FromDiagnostic_FallsBackToContractViolationKind_ForReasonCode()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertyContractViolationKind, "Setter")
			.Add(ArchitectureDiagnosticProperties.PropertyNameRuleKind, "VariableNameMismatch");
		var diagnostic = CreateDiagnostic(Location.None, properties);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic);

		result.ReasonCode.Should().Be("Setter");
	}

	[Fact]
	public void FromDiagnostic_FallsBackToNameRuleKind_ForReasonCode()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertyNameRuleKind, "VariableNameMismatch");
		var diagnostic = CreateDiagnostic(Location.None, properties);

		var result = ArchitectureFindingFactory.FromDiagnostic(diagnostic);

		result.ReasonCode.Should().Be("VariableNameMismatch");
	}

	[Fact]
	public void Finding_WithContextPrefix_AppendsExistingContext()
	{
		var finding = new ArchitectureFinding(ArchitectureFindingSeverity.Error, "ARCH001", "message", "Kitchen.cs:12");

		var result = finding.WithContextPrefix("Project A");

		result.Context.Should().Be("Project A - Kitchen.cs:12");
	}

	[Fact]
	public void Finding_WithContextPrefix_UsesPrefix_WhenContextIsEmpty()
	{
		var finding = new ArchitectureFinding(ArchitectureFindingSeverity.Info, "ARCH001", "message", string.Empty);

		var result = finding.WithContextPrefix("Project A");

		result.Context.Should().Be("Project A");
		result.SeverityText.Should().Be("Info");
		result.Category.Should().Be("ARCH001");
	}

	[Theory]
	[InlineData(DiagnosticSeverity.Hidden, ArchitectureFindingSeverity.Info)]
	[InlineData(DiagnosticSeverity.Info, ArchitectureFindingSeverity.Info)]
	[InlineData(DiagnosticSeverity.Warning, ArchitectureFindingSeverity.Warning)]
	[InlineData(DiagnosticSeverity.Error, ArchitectureFindingSeverity.Error)]
	public void SeverityMapping_FollowsDiagnosticSeverity(DiagnosticSeverity severity, ArchitectureFindingSeverity expected)
	{
		var result = ArchitectureFindingSeverityExtensions.FromDiagnosticSeverity(severity);

		result.Should().Be(expected);
	}

	private static Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties)
	{
		var descriptor = new DiagnosticDescriptor(
			"ARCH001",
			"Title",
			"Rule message",
			"Architecture",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);
		var result = Diagnostic.Create(
			descriptor,
			location,
			additionalLocations: null,
			properties: properties,
			messageArgs: Array.Empty<object>());

		return result;
	}
}
