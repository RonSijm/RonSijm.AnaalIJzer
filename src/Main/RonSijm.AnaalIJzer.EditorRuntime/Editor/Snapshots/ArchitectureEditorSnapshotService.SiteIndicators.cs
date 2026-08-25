using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddSiteIndicators(SyntaxNode node, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		switch (node)
		{
			case ConstructorDeclarationSyntax constructor when constructor.Parent is TypeDeclarationSyntax:
				foreach (var parameter in constructor.ParameterList.Parameters)
				{
					AddParameterDependency(parameter, constructor, DependencySites.Constructor, semanticModel, config, paletteSlots, indicators, cancellationToken);
				}
				break;
			case TypeDeclarationSyntax typeDeclaration:
				AddPrimaryConstructorDependencies(typeDeclaration, semanticModel, config, paletteSlots, indicators, cancellationToken);
				AddBaseListDependencies(typeDeclaration, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case MethodDeclarationSyntax method when method.Parent is TypeDeclarationSyntax:
				AddTypeDependency(method, method.ReturnType.Span, semanticModel.GetTypeInfo(method.ReturnType, cancellationToken).Type, DependencySites.MethodReturn, semanticModel, config, paletteSlots, indicators, cancellationToken);
				foreach (var parameter in method.ParameterList.Parameters)
				{
					AddParameterDependency(parameter, method, DependencySites.Method, semanticModel, config, paletteSlots, indicators, cancellationToken);
				}
				break;
			case FieldDeclarationSyntax field:
				AddTypeDependency(field, field.Declaration.Type.Span, semanticModel.GetTypeInfo(field.Declaration.Type, cancellationToken).Type, DependencySites.Field, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case PropertyDeclarationSyntax property:
				AddTypeDependency(property, property.Type.Span, semanticModel.GetTypeInfo(property.Type, cancellationToken).Type, DependencySites.Property, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case LocalDeclarationStatementSyntax local:
				AddLocalDependencies(local, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case ObjectCreationExpressionSyntax objectCreation:
				AddTypeDependency(objectCreation, objectCreation.Type.Span, semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type, DependencySites.New, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case ImplicitObjectCreationExpressionSyntax implicitCreation:
				AddTypeDependency(implicitCreation, implicitCreation.Span, semanticModel.GetTypeInfo(implicitCreation, cancellationToken).Type, DependencySites.New, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case InvocationExpressionSyntax invocation:
				AddInvocationDependencies(invocation, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case AttributeSyntax attribute:
				AddAttributeDependency(attribute, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
			case MemberAccessExpressionSyntax memberAccess:
				AddStaticMemberDependency(memberAccess, semanticModel, config, paletteSlots, indicators, cancellationToken);
				break;
		}
	}

	private static void AddPrimaryConstructorDependencies(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
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
			AddParameterDependency(parameter, typeDeclaration, DependencySites.Constructor, semanticModel, config, paletteSlots, indicators, cancellationToken);
		}
	}

	private static void AddBaseListDependencies(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		foreach (var baseType in typeDeclaration.BaseList?.Types ?? [])
		{
			var type = semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type;
			if (type is null)
			{
				continue;
			}

			var site = type.TypeKind == TypeKind.Interface && typeDeclaration is not InterfaceDeclarationSyntax
				? DependencySites.InterfaceImplementation
				: DependencySites.Inheritance;
			AddTypeDependency(typeDeclaration, baseType.Type.Span, type, site, semanticModel, config, paletteSlots, indicators, cancellationToken);
		}
	}
}
