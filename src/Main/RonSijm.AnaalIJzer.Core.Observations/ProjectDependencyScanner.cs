using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public static partial class ProjectDependencyScanner
{
	public static IReadOnlyList<ProjectDependencyObservation> Scan(Compilation compilation, Func<INamedTypeSymbol, string?> resolveLayer, CancellationToken cancellationToken)
	{
		var observations = new List<ProjectDependencyObservation>();
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (GeneratedCodeDetector.IsGenerated(syntaxTree, cancellationToken))
			{
				continue;
			}

			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			foreach (var node in syntaxTree.GetRoot(cancellationToken).DescendantNodes())
			{
				switch (node)
				{
					case ConstructorDeclarationSyntax constructor when constructor.Parent is TypeDeclarationSyntax:
						foreach (var parameter in constructor.ParameterList.Parameters)
						{
							AddParameterDependency(parameter, constructor, DependencySites.Constructor, semanticModel, resolveLayer, observations, cancellationToken);
						}
						break;
					case TypeDeclarationSyntax typeDeclaration:
						var parameterList = typeDeclaration switch
						{
							ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
							StructDeclarationSyntax structDeclaration => structDeclaration.ParameterList,
							RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
							_ => null
						};
						foreach (var parameter in parameterList?.Parameters ?? [])
						{
							AddParameterDependency(parameter, typeDeclaration, DependencySites.Constructor, semanticModel, resolveLayer, observations, cancellationToken);
						}
						foreach (var baseType in typeDeclaration.BaseList?.Types ?? [])
						{
							var type = semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type;
							var site = GetBaseListDependencySite(typeDeclaration, type);
							AddTypeDependency(typeDeclaration, type, site, semanticModel, resolveLayer, observations, cancellationToken);
						}
						break;
					case MethodDeclarationSyntax method when method.Parent is TypeDeclarationSyntax:
						AddTypeDependency(method, semanticModel.GetTypeInfo(method.ReturnType, cancellationToken).Type, DependencySites.MethodReturn, semanticModel, resolveLayer, observations, cancellationToken);
						foreach (var parameter in method.ParameterList.Parameters)
						{
							AddParameterDependency(parameter, method, DependencySites.Method, semanticModel, resolveLayer, observations, cancellationToken);
						}
						break;
					case FieldDeclarationSyntax field:
						AddTypeDependency(field, semanticModel.GetTypeInfo(field.Declaration.Type, cancellationToken).Type, DependencySites.Field, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case PropertyDeclarationSyntax property:
						AddTypeDependency(property, semanticModel.GetTypeInfo(property.Type, cancellationToken).Type, DependencySites.Property, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case LocalDeclarationStatementSyntax local:
						AddLocalDependencies(local, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case ObjectCreationExpressionSyntax objectCreation:
						AddTypeDependency(objectCreation, semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type, DependencySites.New, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case ImplicitObjectCreationExpressionSyntax implicitCreation:
						AddTypeDependency(implicitCreation, semanticModel.GetTypeInfo(implicitCreation, cancellationToken).Type, DependencySites.New, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case InvocationExpressionSyntax invocation:
						AddInvocationDependencies(invocation, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case AttributeSyntax attribute:
						AddAttributeDependency(attribute, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case MemberAccessExpressionSyntax memberAccess:
						AddStaticMemberDependency(memberAccess, semanticModel, resolveLayer, observations, cancellationToken);
						break;
				}
			}
		}

		return observations;
	}
}
