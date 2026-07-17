using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ArchitectureDependencyGraphEvidence CreateProjectEvidence(Compilation compilation, ProjectAnalyzerConfig config, CancellationToken cancellationToken)
	{
		var types = ImmutableArray.CreateBuilder<ArchitectureDependencyGraphTypeEvidence>();
		var dependencies = ImmutableArray.CreateBuilder<ArchitectureDependencyGraphDependencyEvidence>();
		var seenTypes = new HashSet<string>(StringComparer.Ordinal);
		var seenDependencies = new HashSet<string>(StringComparer.Ordinal);
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (IsGenerated(syntaxTree, cancellationToken))
			{
				continue;
			}

			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			var root = syntaxTree.GetRoot(cancellationToken);
			foreach (var typeDeclaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
			{
				AddProjectTypeEvidence(typeDeclaration, semanticModel, config, types, seenTypes, cancellationToken);
			}

			foreach (var node in root.DescendantNodes())
			{
				AddProjectDependencyEvidence(node, semanticModel, config, dependencies, seenDependencies, cancellationToken);
			}
		}

		var result = new ArchitectureDependencyGraphEvidence(types.ToImmutable(), dependencies.ToImmutable());

		return result;
	}

	private static void AddProjectTypeEvidence(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphTypeEvidence>.Builder types, HashSet<string> seenTypes, CancellationToken cancellationToken)
	{
		if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol typeSymbol)
		{
			return;
		}

		var match = config.FindLayer(typeSymbol.Name, typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, typeSymbol);
		if (match is null || match.Value.Layer.IsForbidden)
		{
			return;
		}

		var location = typeDeclaration.Identifier.GetLocation();
		var filePath = GetLocationPath(location);
		var lineNumber = GetLineNumber(location);
		var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
		var key = match.Value.Layer.Name + "|" + fullTypeName + "|" + filePath + "|" + lineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
		if (!seenTypes.Add(key))
		{
			return;
		}

		types.Add(new ArchitectureDependencyGraphTypeEvidence(
			match.Value.Layer.Name,
			typeSymbol.Name,
			fullTypeName,
			filePath,
			lineNumber));
	}

	private static void AddProjectDependencyEvidence(SyntaxNode node, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		switch (node)
		{
			case ConstructorDeclarationSyntax constructor when constructor.Parent is TypeDeclarationSyntax:
				foreach (var parameter in constructor.ParameterList.Parameters)
				{
					AddProjectParameterDependencyEvidence(parameter, constructor, DependencySites.Constructor, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				}
				break;
			case TypeDeclarationSyntax typeDeclaration:
				AddProjectPrimaryConstructorDependencyEvidence(typeDeclaration, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				AddProjectBaseListDependencyEvidence(typeDeclaration, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case MethodDeclarationSyntax method when method.Parent is TypeDeclarationSyntax:
				AddProjectTypeDependencyEvidence(method, semanticModel.GetTypeInfo(method.ReturnType, cancellationToken).Type, DependencySites.MethodReturn, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				foreach (var parameter in method.ParameterList.Parameters)
				{
					AddProjectParameterDependencyEvidence(parameter, method, DependencySites.Method, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				}
				break;
			case FieldDeclarationSyntax field:
				AddProjectTypeDependencyEvidence(field, semanticModel.GetTypeInfo(field.Declaration.Type, cancellationToken).Type, DependencySites.Field, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case PropertyDeclarationSyntax property:
				AddProjectTypeDependencyEvidence(property, semanticModel.GetTypeInfo(property.Type, cancellationToken).Type, DependencySites.Property, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case LocalDeclarationStatementSyntax local:
				AddProjectLocalDependencyEvidence(local, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case ObjectCreationExpressionSyntax objectCreation:
				AddProjectTypeDependencyEvidence(objectCreation, semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type, DependencySites.New, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case ImplicitObjectCreationExpressionSyntax implicitCreation:
				AddProjectTypeDependencyEvidence(implicitCreation, semanticModel.GetTypeInfo(implicitCreation, cancellationToken).Type, DependencySites.New, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case InvocationExpressionSyntax invocation:
				AddProjectInvocationDependencyEvidence(invocation, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case AttributeSyntax attribute:
				AddProjectAttributeDependencyEvidence(attribute, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
			case MemberAccessExpressionSyntax memberAccess:
				AddProjectStaticMemberDependencyEvidence(memberAccess, semanticModel, config, dependencies, seenDependencies, cancellationToken);
				break;
		}
	}

	private static void AddProjectPrimaryConstructorDependencyEvidence(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
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
			AddProjectParameterDependencyEvidence(parameter, typeDeclaration, DependencySites.Constructor, semanticModel, config, dependencies, seenDependencies, cancellationToken);
		}
	}

	private static void AddProjectBaseListDependencyEvidence(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
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
			AddProjectTypeDependencyEvidence(typeDeclaration, type, site, semanticModel, config, dependencies, seenDependencies, cancellationToken);
		}
	}

	private static void AddProjectParameterDependencyEvidence(ParameterSyntax parameter, SyntaxNode callerNode, string site, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken) as IParameterSymbol;
		AddProjectTypeDependencyEvidence(callerNode, parameterSymbol?.Type, site, semanticModel, config, dependencies, seenDependencies, cancellationToken);
	}

	private static void AddProjectLocalDependencyEvidence(LocalDeclarationStatementSyntax local, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		var type = semanticModel.GetTypeInfo(local.Declaration.Type, cancellationToken).Type;
		if (type is not null && type.TypeKind != TypeKind.Error)
		{
			AddProjectTypeDependencyEvidence(local, type, DependencySites.Local, semanticModel, config, dependencies, seenDependencies, cancellationToken);
			return;
		}

		foreach (var variable in local.Declaration.Variables)
		{
			if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is ILocalSymbol localSymbol)
			{
				AddProjectTypeDependencyEvidence(local, localSymbol.Type, DependencySites.Local, semanticModel, config, dependencies, seenDependencies, cancellationToken);
			}
		}
	}

	private static void AddProjectInvocationDependencyEvidence(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
		{
			var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
			if (staticContainer is not null)
			{
				AddProjectTypeDependencyEvidence(invocation, staticContainer, DependencySites.StaticMember, semanticModel, config, dependencies, seenDependencies, cancellationToken);
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
			AddProjectTypeDependencyEvidence(invocation, semanticModel.GetTypeInfo(argument, cancellationToken).Type, DependencySites.GenericInvocation, semanticModel, config, dependencies, seenDependencies, cancellationToken);
		}
	}

	private static void AddProjectAttributeDependencyEvidence(AttributeSyntax attribute, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor)
		{
			AddProjectTypeDependencyEvidence(attribute, constructor.ContainingType, DependencySites.Attribute, semanticModel, config, dependencies, seenDependencies, cancellationToken);
		}
	}

	private static void AddProjectStaticMemberDependencyEvidence(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
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
			AddProjectTypeDependencyEvidence(memberAccess, containingType, DependencySites.StaticMember, semanticModel, config, dependencies, seenDependencies, cancellationToken);
		}
	}

	private static void AddProjectTypeDependencyEvidence(SyntaxNode callerNode, ITypeSymbol? dependencyType, string site, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Builder dependencies, HashSet<string> seenDependencies, CancellationToken cancellationToken)
	{
		if (dependencyType is null || TryGetCaller(callerNode, semanticModel, config, cancellationToken) is not { } caller)
		{
			return;
		}

		var callerDeclaration = callerNode.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		var callerSymbol = callerDeclaration is null
			? null
			: semanticModel.GetDeclaredSymbol(callerDeclaration, cancellationToken);
		var seenDependencyNames = new HashSet<string>(StringComparer.Ordinal);
		var index = 0;
		foreach (var current in EnumerateTypeAndGenericArguments(dependencyType))
		{
			var isOuter = index++ == 0;
			var effectiveSite = isOuter ? site : DependencySites.GenericArgument;
			if (string.IsNullOrEmpty(current.Name) || IsIgnoredRecognitionType(current))
			{
				continue;
			}

			if (callerSymbol is not null && SymbolEqualityComparer.Default.Equals(callerSymbol.OriginalDefinition, current.OriginalDefinition))
			{
				continue;
			}

			var dependencyMatch = config.FindLayer(current.Name, current.ContainingNamespace?.ToDisplayString() ?? string.Empty, current);
			if (dependencyMatch is null || !seenDependencyNames.Add(current.Name))
			{
				continue;
			}

			var evidence = CreateProjectDependencyEvidence(callerNode, effectiveSite, caller, current, dependencyMatch.Value, config);
			var key = evidence.CallerLayerPath
			          + "|"
			          + evidence.DependencyLayerPath
			          + "|"
			          + evidence.CallerTypeName
			          + "|"
			          + evidence.DependencyTypeName
			          + "|"
			          + evidence.Site
			          + "|"
			          + evidence.FilePath
			          + "|"
			          + evidence.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if (seenDependencies.Add(key))
			{
				dependencies.Add(evidence);
			}
		}
	}

	private static ArchitectureDependencyGraphDependencyEvidence CreateProjectDependencyEvidence(SyntaxNode callerNode, string site, CallerInfo caller, ITypeSymbol dependencyType, LayerMatch dependencyMatch, ProjectAnalyzerConfig config)
	{
		var dependencyLayer = dependencyMatch.Layer;
		var status = ArchitectureDependencySiteStatus.Allowed;
		string? diagnosticId = null;
		var reason = "allowed by configured dependency rules";
		if (dependencyLayer.IsForbidden)
		{
			status = ArchitectureDependencySiteStatus.TypePolicyViolation;
			diagnosticId = ArchitecturalDiagnosticIds.ForbiddenDependency;
			reason = dependencyLayer.Comment is null
				? "the type matches a global <Forbidden> rule"
				: "the type matches a global <Forbidden> rule: " + dependencyLayer.Comment;
		}
		else if (config.EvaluateTypePolicy(dependencyMatch, dependencyType.Name, dependencyType.ContainingNamespace?.ToDisplayString() ?? string.Empty, dependencyType) is { } policyViolation)
		{
			status = ArchitectureDependencySiteStatus.TypePolicyViolation;
			diagnosticId = ArchitecturalDiagnosticIds.ForbiddenDependency;
			reason = policyViolation.Reason;
		}
		else
		{
			var edgeEvaluation = config.Graph.EvaluateDependency(caller.Match, dependencyMatch, site);
			if (!edgeEvaluation.IsAllowed)
			{
				status = GetDeniedStatus(caller.LayerPath, dependencyLayer.Name, edgeEvaluation, config);
				diagnosticId = status switch
				{
					ArchitectureDependencySiteStatus.WrongDirection => ArchitecturalDiagnosticIds.WrongDirectionDependency,
					ArchitectureDependencySiteStatus.SameLayer => ArchitecturalDiagnosticIds.SameLayerDependency,
					_ => ArchitecturalDiagnosticIds.IllegalLevelDependency
				};
				reason = status == ArchitectureDependencySiteStatus.SameLayer && !edgeEvaluation.IsDeniedBySiteFilter
					? $"types in the same layer ('{caller.LayerPath}') may not depend on each other"
					: status == ArchitectureDependencySiteStatus.WrongDirection && !edgeEvaluation.IsDeniedBySiteFilter
						? $"this dependency goes the wrong direction - the reverse ('{dependencyLayer.Name}' -> '{caller.LayerPath}') is configured"
						: edgeEvaluation.DenialReason;
			}
		}

		var location = callerNode.GetLocation();
		var result = new ArchitectureDependencyGraphDependencyEvidence(
			caller.LayerPath,
			dependencyLayer.Name,
			caller.TypeName,
			dependencyType.Name,
			site,
			status,
			diagnosticId,
			reason,
			GetLocationPath(location),
			GetLineNumber(location));

		return result;
	}

	private static string GetLocationPath(Location location)
	{
		var lineSpan = location.GetLineSpan();
		var result = string.IsNullOrWhiteSpace(lineSpan.Path)
			? location.SourceTree?.FilePath ?? string.Empty
			: lineSpan.Path;

		return result;
	}

	private static int GetLineNumber(Location location)
	{
		var lineSpan = location.GetLineSpan();
		var result = lineSpan.StartLinePosition.Line + 1;

		return result;
	}
}
