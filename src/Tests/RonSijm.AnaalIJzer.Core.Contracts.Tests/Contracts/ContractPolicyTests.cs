using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RonSijm.AnaalIJzer.Core.Contracts.Tests.Contracts;

public sealed class ContractPolicyTests
{
	[Fact]
	public void ContractMemberKindParser_ParsesKnownValuesInCanonicalOrder()
	{
		ContractMemberKindParser.CanonicalOrder.Should().Equal(
			ContractMemberKind.Constructor,
			ContractMemberKind.Method,
			ContractMemberKind.Property,
			ContractMemberKind.Event,
			ContractMemberKind.Field,
			ContractMemberKind.Operator,
			ContractMemberKind.Conversion);

		ContractMemberKindParser.TryParse("property", out var result).Should().BeTrue();
		result.Should().Be(ContractMemberKind.Property);
	}

	[Fact]
	public void ContractPropertyAccessorParser_ParsesKnownValuesInCanonicalOrder()
	{
		ContractPropertyAccessorParser.CanonicalOrder.Should().Equal(
			ContractPropertyAccessor.Get,
			ContractPropertyAccessor.Set,
			ContractPropertyAccessor.Init);

		ContractPropertyAccessorParser.TryParse("init", out var result).Should().BeTrue();
		result.Should().Be(ContractPropertyAccessor.Init);
	}

	[Fact]
	public void ContractPolicy_Evaluate_ReturnsViolationForDisallowedPropertyAccessor()
	{
		var policy = new ContractPolicy(
			"Application/Contracts",
			ImmutableHashSet.Create("Interface"),
			ImmutableHashSet.Create(ContractMemberKind.Property),
			ImmutableHashSet.Create(ContractPropertyAccessor.Get),
			true,
			false,
			false,
			false,
			null,
			"Architecture.anl",
			10,
			5);
		var shape = new ContractDeclarationShape(
			"Token",
			"Interface",
			ContractMemberKind.Property,
			ImmutableHashSet.Create(ContractPropertyAccessor.Get, ContractPropertyAccessor.Set),
			false,
			false,
			false);

		var result = policy.Evaluate(shape);

		result.Should().NotBeNull();
		result!.Value.ViolationKind.Should().Be(ContractViolationKind.DisallowedPropertyAccessor);
		result.Value.Reason.Should().Contain("allows only property accessors");
	}

	[Fact]
	public void ContractDeclarationShapeFactory_CreateMemberShapes_CapturesInitAccessor()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tree = CSharpSyntaxTree.ParseText("""
			namespace Demo;
			public interface IToken
			{
				string Value { get; init; }
			}
			""", cancellationToken: cancellationToken);
		var compilation = CSharpCompilation.Create(
			"Demo",
			[tree],
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
			]);
		var type = compilation.GetTypeByMetadataName("Demo.IToken")!;
		var property = type.GetMembers().OfType<IPropertySymbol>().Single();

		var result = ContractDeclarationShapeFactory.CreateMemberShapes(property, cancellationToken).Single();

		result.Shape.MemberKind.Should().Be(ContractMemberKind.Property);
		result.Shape.PropertyAccessors.Should().Contain(ContractPropertyAccessor.Get);
		result.Shape.PropertyAccessors.Should().Contain(ContractPropertyAccessor.Init);
		result.Location.Should().NotBe(Location.None);
	}
}
