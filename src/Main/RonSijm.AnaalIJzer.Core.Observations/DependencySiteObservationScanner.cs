using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.Observations;

public static class DependencySiteObservationScanner
{
    public static IReadOnlyList<DependencySiteObservation> Scan(Compilation compilation, CancellationToken cancellationToken)
    {
        var result = Scan(compilation, GeneratedCodeAnalysisScope.Exclude, cancellationToken);

        return result;
    }

    public static IReadOnlyList<DependencySiteObservation> Scan(Compilation compilation, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
    {
        var observations = new List<DependencySiteObservation>();
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!generatedCodeScope.ShouldAnalyze(syntaxTree, cancellationToken))
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var node in syntaxTree.GetRoot(cancellationToken).DescendantNodes())
            {
                AnalyzeNode(node, semanticModel, observations, cancellationToken);
            }
        }

        return observations;
    }

    private static void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case ConstructorDeclarationSyntax { Parent: TypeDeclarationSyntax } constructor:
                foreach (var parameter in constructor.ParameterList.Parameters)
                {
                    AddParameterDependency(parameter, constructor, DependencySites.Constructor, semanticModel, observations, cancellationToken);
                }
                break;
            case TypeDeclarationSyntax typeDeclaration:
                AnalyzeTypeDeclaration(typeDeclaration, semanticModel, observations, cancellationToken);
                break;
            case MethodDeclarationSyntax { Parent: TypeDeclarationSyntax } method:
                AddTypeDependencies(method, semanticModel.GetTypeInfo(method.ReturnType, cancellationToken).Type, DependencySites.MethodReturn, semanticModel, observations, cancellationToken);
                foreach (var parameter in method.ParameterList.Parameters)
                {
                    AddParameterDependency(parameter, method, DependencySites.Method, semanticModel, observations, cancellationToken);
                }
                break;
            case FieldDeclarationSyntax field:
                AddTypeDependencies(field, semanticModel.GetTypeInfo(field.Declaration.Type, cancellationToken).Type, DependencySites.Field, semanticModel, observations, cancellationToken);
                break;
            case PropertyDeclarationSyntax property:
                AddTypeDependencies(property, semanticModel.GetTypeInfo(property.Type, cancellationToken).Type, DependencySites.Property, semanticModel, observations, cancellationToken);
                break;
            case LocalDeclarationStatementSyntax local:
                AddLocalDependencies(local, semanticModel, observations, cancellationToken);
                break;
            case ObjectCreationExpressionSyntax objectCreation:
                AddTypeDependencies(objectCreation, semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type, DependencySites.New, semanticModel, observations, cancellationToken);
                break;
            case ImplicitObjectCreationExpressionSyntax implicitCreation:
                AddTypeDependencies(implicitCreation, semanticModel.GetTypeInfo(implicitCreation, cancellationToken).Type, DependencySites.New, semanticModel, observations, cancellationToken);
                break;
            case InvocationExpressionSyntax invocation:
                AddInvocationDependencies(invocation, semanticModel, observations, cancellationToken);
                break;
            case AttributeSyntax attribute:
                AddAttributeDependency(attribute, semanticModel, observations, cancellationToken);
                break;
            case MemberAccessExpressionSyntax memberAccess:
                AddStaticMemberDependency(memberAccess, semanticModel, observations, cancellationToken);
                break;
        }
    }

    private static void AnalyzeTypeDeclaration(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        var parameterList = typeDeclaration switch
        {
            ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
            StructDeclarationSyntax structDeclaration => structDeclaration.ParameterList,
            RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
            _ => null
        };
        foreach (var parameter in parameterList?.Parameters ?? [])
        {
            AddParameterDependency(parameter, typeDeclaration, DependencySites.Constructor, semanticModel, observations, cancellationToken);
        }

        foreach (var baseType in typeDeclaration.BaseList?.Types ?? [])
        {
            var type = semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type;
            var site = GetBaseListDependencySite(typeDeclaration, type);
            AddTypeDependencies(typeDeclaration, type, site, semanticModel, observations, cancellationToken);
        }
    }

    private static void AddParameterDependency(ParameterSyntax parameter, SyntaxNode callerNode, string site, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken) as IParameterSymbol;
        AddTypeDependencies(callerNode, parameterSymbol?.Type, site, semanticModel, observations, cancellationToken);
    }

    private static void AddInvocationDependencies(InvocationExpressionSyntax invocation, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
        {
            var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
            if (staticContainer is not null)
            {
                AddTypeDependencies(invocation, staticContainer, DependencySites.StaticMember, semanticModel, observations, cancellationToken);
            }
        }

        var generic = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
            GenericNameSyntax genericName => genericName,
            _ => null
        };
        if (generic is null)
        {
            return;
        }

        foreach (var argument in generic.TypeArgumentList.Arguments)
        {
            AddTypeDependencies(invocation, semanticModel.GetTypeInfo(argument, cancellationToken).Type, DependencySites.GenericInvocation, semanticModel, observations, cancellationToken);
        }
    }

    private static void AddAttributeDependency(AttributeSyntax attribute, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor)
        {
            AddTypeDependencies(attribute, constructor.ContainingType, DependencySites.Attribute, semanticModel, observations, cancellationToken);
        }
    }

    private static void AddStaticMemberDependency(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
        var containingType = symbol switch
        {
            IPropertySymbol { IsStatic: true } property => property.ContainingType,
            IFieldSymbol { IsStatic: true } field => field.ContainingType,
            IEventSymbol { IsStatic: true } @event => @event.ContainingType,
            _ => null
        };
        if (containingType is not null)
        {
            AddTypeDependencies(memberAccess, containingType, DependencySites.StaticMember, semanticModel, observations, cancellationToken);
        }
    }

    private static void AddLocalDependencies(LocalDeclarationStatementSyntax local, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        var type = semanticModel.GetTypeInfo(local.Declaration.Type, cancellationToken).Type;
        if (type is not null && type.TypeKind != TypeKind.Error)
        {
            AddTypeDependencies(local, type, DependencySites.Local, semanticModel, observations, cancellationToken);
            return;
        }

        foreach (var variable in local.Declaration.Variables)
        {
            if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is ILocalSymbol localSymbol)
            {
                AddTypeDependencies(local, localSymbol.Type, DependencySites.Local, semanticModel, observations, cancellationToken);
            }
        }
    }

    private static void AddTypeDependencies(SyntaxNode callerNode, ITypeSymbol? dependencyType, string site, SemanticModel semanticModel, List<DependencySiteObservation> observations, CancellationToken cancellationToken)
    {
        if (dependencyType is null)
        {
            return;
        }

        var callerDeclaration = callerNode.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (callerDeclaration is null || semanticModel.GetDeclaredSymbol(callerDeclaration, cancellationToken) is not INamedTypeSymbol callerType)
        {
            return;
        }

        callerType = callerType.OriginalDefinition;
        var index = 0;
        foreach (var currentType in EnumerateTypeAndGenericArguments(dependencyType))
        {
            var effectiveSite = index++ == 0 ? site : DependencySites.GenericArgument;
            if (currentType is not INamedTypeSymbol namedType)
            {
                continue;
            }

            observations.Add(new DependencySiteObservation(callerType, namedType.OriginalDefinition, effectiveSite, callerNode.GetLocation()));
        }
    }

    private static string GetBaseListDependencySite(TypeDeclarationSyntax typeDeclaration, ITypeSymbol? type)
    {
        var result = type?.TypeKind == TypeKind.Interface && typeDeclaration is not InterfaceDeclarationSyntax
            ? DependencySites.InterfaceImplementation
            : DependencySites.Inheritance;

        return result;
    }

    private static IEnumerable<ITypeSymbol> EnumerateTypeAndGenericArguments(ITypeSymbol root)
    {
        var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var stack = new Stack<ITypeSymbol>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            yield return current;
            if (current is INamedTypeSymbol namedType)
            {
                for (var index = namedType.TypeArguments.Length - 1; index >= 0; index--)
                {
                    stack.Push(namedType.TypeArguments[index]);
                }
            }
            else if (current is IArrayTypeSymbol arrayType)
            {
                stack.Push(arrayType.ElementType);
            }
        }
    }
}