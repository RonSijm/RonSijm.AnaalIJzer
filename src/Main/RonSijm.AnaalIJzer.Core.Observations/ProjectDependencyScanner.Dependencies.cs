using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public static partial class ProjectDependencyScanner
{
	private static void AddParameterDependency(ParameterSyntax parameter, SyntaxNode callerNode, string site, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken) as IParameterSymbol;
		AddTypeDependency(callerNode, parameterSymbol?.Type, site, semanticModel, resolveLayer, observations, cancellationToken);
	}

	private static void AddInvocationDependencies(InvocationExpressionSyntax invocation, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
		{
			var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
			if (staticContainer is not null)
			{
				AddTypeDependency(invocation, staticContainer, DependencySites.StaticMember, semanticModel, resolveLayer, observations, cancellationToken);
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
			AddTypeDependency(invocation, semanticModel.GetTypeInfo(argument, cancellationToken).Type, DependencySites.GenericInvocation, semanticModel, resolveLayer, observations, cancellationToken);
		}
	}

	private static void AddAttributeDependency(AttributeSyntax attribute, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor)
		{
			AddTypeDependency(attribute, constructor.ContainingType, DependencySites.Attribute, semanticModel, resolveLayer, observations, cancellationToken);
		}
	}

	private static void AddStaticMemberDependency(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
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
			AddTypeDependency(memberAccess, containingType, DependencySites.StaticMember, semanticModel, resolveLayer, observations, cancellationToken);
		}
	}

	private static void AddTypeDependency(SyntaxNode callerNode, ITypeSymbol? dependencyType, string site, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
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
		var callerLayer = resolveLayer(callerType);
		if (callerLayer is null)
		{
			return;
		}

		var index = 0;
		foreach (var currentType in EnumerateTypeAndGenericArguments(dependencyType))
		{
			var effectiveSite = index++ == 0 ? site : DependencySites.GenericArgument;
			if (currentType is not INamedTypeSymbol namedType || namedType.Name == callerType.Name)
			{
				continue;
			}

			namedType = namedType.OriginalDefinition;
			var dependencyLayer = resolveLayer(namedType);
			if (dependencyLayer is null)
			{
				continue;
			}

			observations.Add(new ProjectDependencyObservation(callerType, callerLayer, namedType, dependencyLayer, effectiveSite, callerNode.GetLocation()));
		}
	}
}
