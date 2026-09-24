namespace RonSijm.AnaalIJzer.Core.OperationContracts.Tests.Evaluation;

public sealed class OperationContractEvaluatorTests
{
    [Fact]
    public void EvaluateOwner_ReportsEveryConfiguredShapeViolation()
    {
        var owner = GetMethod(
            """
			public sealed class PizzaKitchen
			{
				public string PlacePizzaOrder(UnrelatedRequest request) => "";
			}

			public sealed class UnrelatedRequest { }
			""");
        var definition = CreateDefinition(
            requestTypeName: "PlacePizzaOrderRequest",
            responseTypeName: "PlacePizzaOrderResponse",
            allowedOwnerLayers: ["Application"]);

        var evaluations = OperationContractEvaluator.EvaluateOwner(definition, owner, "Controller");

        evaluations.Select(evaluation => evaluation.ViolationKind).Should().BeEquivalentTo(
        [
            OperationContractViolationKind.OwnerOutsideAllowedLayer,
            OperationContractViolationKind.OwnerMissingRequest,
            OperationContractViolationKind.OwnerInvalidResponse
        ]);
    }

    [Fact]
    public void EvaluateEntryPoint_AllowsTheConfiguredRequestResponseAndDirectOwnerInvocation()
    {
        var entryPoint = GetMethod(
            """
			public sealed class PizzaOrderController
			{
				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request) => new();
			}

			public sealed class PlacePizzaOrderRequest { }
			public sealed class PlacePizzaOrderResponse { }
			""",
            "PizzaOrderController");
        var definition = CreateDefinition(
            requestTypeName: "PlacePizzaOrderRequest",
            responseTypeName: "PlacePizzaOrderResponse",
            allowedEntryPointLayers: ["Controller"]);

        var evaluations = OperationContractEvaluator.EvaluateEntryPoint(definition, entryPoint, "Controller", invokesOwner: true);

        evaluations.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Action work = () => kitchen.PlacePizzaOrder(request);")]
    [InlineData("void Later() => kitchen.PlacePizzaOrder(request); Later();")]
    public void DirectlyInvokesOwner_IgnoresCallsInsideNestedFunctions(string body)
    {
        const string source = """
			using System;

			public sealed class PizzaOrderController
			{
				private readonly PizzaKitchen kitchen = new();

				public void PlacePizzaOrder(PlacePizzaOrderRequest request)
				{
					BODY
				}
			}

			public sealed class PizzaKitchen
			{
				public void PlacePizzaOrder(PlacePizzaOrderRequest request) { }
			}

			public sealed class PlacePizzaOrderRequest { }
			""";
        var (semanticModel, entryPoint) = GetMethodDeclaration(source.Replace("BODY", body, StringComparison.Ordinal), "PizzaOrderController");
        var definition = CreateDefinition();

        var directlyInvokesOwner = OperationContractInvocationInspector.DirectlyInvokesOwner(semanticModel, entryPoint, definition, CancellationToken.None);

        directlyInvokesOwner.Should().BeFalse();
    }

    [Fact]
    public void DirectlyInvokesOwner_RecognizesTheSelectedOwnerInTheEntryPointBody()
    {
        const string source = """
			public sealed class PizzaOrderController
			{
				private readonly PizzaKitchen kitchen = new();

				public void PlacePizzaOrder(PlacePizzaOrderRequest request)
				{
					kitchen.PlacePizzaOrder(request);
				}
			}

			public sealed class PizzaKitchen
			{
				public void PlacePizzaOrder(PlacePizzaOrderRequest request) { }
			}

			public sealed class PlacePizzaOrderRequest { }
			""";
        var (semanticModel, entryPoint) = GetMethodDeclaration(source, "PizzaOrderController");
        var definition = CreateDefinition();

        var directlyInvokesOwner = OperationContractInvocationInspector.DirectlyInvokesOwner(semanticModel, entryPoint, definition, CancellationToken.None);

        directlyInvokesOwner.Should().BeTrue();
    }

    private static OperationContractDefinition CreateDefinition(
        string? requestTypeName = null,
        string? responseTypeName = null,
        IEnumerable<string>? allowedOwnerLayers = null,
        IEnumerable<string>? allowedEntryPointLayers = null)
    {
        var ownerMatcher = new SemanticDeclarationMatcher(
            [new MatchCondition(MatchKind.Equals, "PizzaKitchen")],
            [new MatchCondition(MatchKind.Equals, "PlacePizzaOrder", MatchOperand.Declaration)],
            ImmutableHashSet.Create(SemanticOperationMemberKind.Method));
        var owner = new OperationContractDeclarationSelector(ownerMatcher, "PlacePizzaOrder", "Architecture.anl", 1, 1);
        var result = new OperationContractDefinition(
            "PlacePizzaOrder",
            owner,
            [],
            requestTypeName is null ? null : new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, requestTypeName),
            responseTypeName is null ? null : new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, responseTypeName),
            allowedOwnerLayers is null ? null : allowedOwnerLayers.ToImmutableHashSet(StringComparer.Ordinal),
            allowedEntryPointLayers is null ? null : allowedEntryPointLayers.ToImmutableHashSet(StringComparer.Ordinal),
            null,
            "Architecture.anl",
            1,
            1);

        return result;
    }

    private static IMethodSymbol GetMethod(string source, string containingTypeName = "PizzaKitchen")
    {
        var (_, method) = GetMethodSymbol(source, containingTypeName);

        return method;
    }

    private static (SemanticModel SemanticModel, MethodDeclarationSyntax Declaration) GetMethodDeclaration(string source, string containingTypeName)
    {
        var (semanticModel, method) = GetMethodSymbol(source, containingTypeName);
        var declaration = method.DeclaringSyntaxReferences.Single().GetSyntax() as MethodDeclarationSyntax;
        declaration.Should().NotBeNull();

        return (semanticModel, declaration!);
    }

    private static (SemanticModel SemanticModel, IMethodSymbol Method) GetMethodSymbol(string source, string containingTypeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "OperationContractTests",
            [syntaxTree],
            TrustedPlatformReferences.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var declaration = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "PlacePizzaOrder" && method.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText == containingTypeName);
        var methodSymbol = semanticModel.GetDeclaredSymbol(declaration);
        methodSymbol.Should().NotBeNull();

        return (semanticModel, methodSymbol!);
    }

    private static readonly Lazy<MetadataReference[]> TrustedPlatformReferences = new(() =>
    {
        var result = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        return result;
    });
}