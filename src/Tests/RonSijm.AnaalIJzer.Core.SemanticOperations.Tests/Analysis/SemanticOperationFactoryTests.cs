using RonSijm.AnaalIJzer.Core.SemanticOperations.Tests.Support;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Tests.Analysis;

public sealed class SemanticOperationFactoryTests
{
	[Fact]
	public void Invocation_UsesTheResolvedGenericMethodAndCaller()
	{
		const string source = """
		                      using ClockAlias = Bakery.Clock;

		                      namespace Bakery;

		                      public static class Clock
		                      {
		                          public static void Tick<T>(T value) { }
		                      }

		                      public sealed class BakingService
		                      {
		                          public void Bake(int minutes)
		                          {
		                              ClockAlias.Tick<int>(minutes);
		                          }
		                      }
		                      """;

		var operation = SemanticOperationTestFactory.GetOperation<InvocationExpressionSyntax>(source);

		operation.Kind.Should().Be(SemanticOperationKind.Invocation);
		operation.SelectedSymbol.Should().BeAssignableTo<IMethodSymbol>().Which.Name.Should().Be("Tick");
		operation.ContainingType!.ToDisplayString().Should().Be("Bakery.Clock");
		operation.CallerSymbol!.Name.Should().Be("Bake");
		operation.IsStaticAccess.Should().BeTrue();
		operation.GenericTypeArguments.Should().ContainSingle().Which.SpecialType.Should().Be(SpecialType.System_Int32);
		operation.Site.Should().Be(DependencySites.StaticMember);
		operation.DisplayName.Should().Contain("Tick");
	}

	[Fact]
	public void MemberReferences_SeparateStaticReadsAndWrites()
	{
		const string source = """
		                      namespace Bakery;

		                      public static class Pantry
		                      {
		                          public static int Quantity;
		                          public static int Available { get; set; }
		                      }

		                      public sealed class BakingService
		                      {
		                          public void Bake()
		                          {
		                              var available = Pantry.Available;
		                              Pantry.Available = available;
		                              var quantity = Pantry.Quantity;
		                              Pantry.Quantity = quantity;
		                          }
		                      }
		                      """;

		var propertyRead = SemanticOperationTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "Available" && node.Parent is EqualsValueClauseSyntax);
		var propertyWrite = SemanticOperationTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "Available" && node.Parent is AssignmentExpressionSyntax);
		var fieldRead = SemanticOperationTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "Quantity" && node.Parent is EqualsValueClauseSyntax);
		var fieldWrite = SemanticOperationTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "Quantity" && node.Parent is AssignmentExpressionSyntax);

		propertyRead.Kind.Should().Be(SemanticOperationKind.PropertyRead);
		propertyWrite.Kind.Should().Be(SemanticOperationKind.PropertyWrite);
		fieldRead.Kind.Should().Be(SemanticOperationKind.FieldRead);
		fieldWrite.Kind.Should().Be(SemanticOperationKind.FieldWrite);
		new[] { propertyRead, propertyWrite, fieldRead, fieldWrite }.Should().OnlyContain(operation => operation.IsStaticAccess);
	}

	[Fact]
	public void CreationConversionAssignmentReturnAndArgument_AreRepresentedSemantically()
	{
		const string source = """
		                      namespace Bakery;

		                      public sealed class Topping { }

		                      public sealed class BakingService
		                      {
		                          public int Bake(double amount)
		                          {
		                              var topping = new Topping();
		                              var count = (int)amount;
		                              count = 2;
		                              Accept(count);
		                              return count;
		                          }

		                          private static void Accept(int value) { }
		                      }
		                      """;

		var creation = SemanticOperationTestFactory.GetOperation<ObjectCreationExpressionSyntax>(source);
		var conversion = SemanticOperationTestFactory.GetOperation<CastExpressionSyntax>(source);
		var assignment = SemanticOperationTestFactory.GetOperation<AssignmentExpressionSyntax>(source);
		var argument = SemanticOperationTestFactory.GetOperation<ArgumentSyntax>(source);
		var returnOperation = SemanticOperationTestFactory.GetOperation<ReturnStatementSyntax>(source);

		creation.Kind.Should().Be(SemanticOperationKind.ObjectCreation);
		creation.ContainingType!.Name.Should().Be("Topping");
		conversion.Kind.Should().Be(SemanticOperationKind.Conversion);
		assignment.Kind.Should().Be(SemanticOperationKind.Assignment);
		assignment.SelectedSymbol.Should().BeAssignableTo<ILocalSymbol>().Which.Name.Should().Be("count");
		argument.Kind.Should().Be(SemanticOperationKind.Argument);
		argument.SelectedSymbol.Should().BeAssignableTo<IParameterSymbol>().Which.Name.Should().Be("value");
		returnOperation.Kind.Should().Be(SemanticOperationKind.Return);
		returnOperation.Site.Should().Be(DependencySites.MethodReturn);
		returnOperation.CallerSymbol!.Name.Should().Be("Bake");
	}

	[Fact]
	public void EventReferences_AreRepresentedAsEventAccess()
	{
		const string source = """
		                      using System;

		                      namespace Bakery;

		                      public static class Oven
		                      {
		                          public static event Action? Ready;
		                      }

		                      public sealed class BakingService
		                      {
		                          public void Bake()
		                          {
		                              Oven.Ready += OnReady;
		                          }

		                          private void OnReady() { }
		                      }
		                      """;

		var operation = SemanticOperationTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "Ready");

		operation.Kind.Should().Be(SemanticOperationKind.EventAccess);
		operation.SelectedSymbol.Should().BeAssignableTo<IEventSymbol>().Which.Name.Should().Be("Ready");
		operation.IsStaticAccess.Should().BeTrue();
	}

	[Fact]
	public void InstanceOperationInAnExpressionBodiedMethodUsesTheMethodReturnSite()
	{
		const string source = """
			                      namespace Bakery;

			                      public sealed class Oven
			                      {
			                          public int ReadTemperature() => 220;
			                      }

			                      public sealed class BakingService
			                      {
			                          public int Bake(Oven oven) => oven.ReadTemperature();
			                      }
			                      """;

		var operation = SemanticOperationTestFactory.GetOperation<InvocationExpressionSyntax>(source, invocation => invocation.ToString() == "oven.ReadTemperature()");

		operation.Kind.Should().Be(SemanticOperationKind.Invocation);
		operation.Site.Should().Be(DependencySites.MethodReturn);
	}

	[Fact]
	public void UnsupportedInvalidOperations_AreIgnored()
	{
		const string source = """
		                      public sealed class BakingService
		                      {
		                          public void Bake()
		                          {
		                              MissingPantry.Refill();
		                          }
		                      }
		                      """;

		var result = SemanticOperationTestFactory.TryCreateOperation<InvocationExpressionSyntax>(source);

		result.Should().BeFalse();
	}
}
