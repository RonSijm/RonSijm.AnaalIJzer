using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Snapshots;

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

	private static void AddParameterDependency(ParameterSyntax parameter, SyntaxNode callerNode, string site, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken) as IParameterSymbol;
		AddTypeDependency(callerNode, (parameter.Type ?? (SyntaxNode)parameter).Span, parameterSymbol?.Type, site, semanticModel, config, paletteSlots, indicators, cancellationToken);
	}

	private static void AddLocalDependencies(LocalDeclarationStatementSyntax local, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		var type = semanticModel.GetTypeInfo(local.Declaration.Type, cancellationToken).Type;
		if (type is not null && type.TypeKind != TypeKind.Error)
		{
			AddTypeDependency(local, local.Declaration.Type.Span, type, DependencySites.Local, semanticModel, config, paletteSlots, indicators, cancellationToken);
			return;
		}

		foreach (var variable in local.Declaration.Variables)
		{
			if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is ILocalSymbol localSymbol)
			{
				AddTypeDependency(local, variable.Identifier.Span, localSymbol.Type, DependencySites.Local, semanticModel, config, paletteSlots, indicators, cancellationToken);
			}
		}
	}

	private static void AddInvocationDependencies(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
		{
			var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
			if (staticContainer is not null)
			{
				var span = invocation.Expression is MemberAccessExpressionSyntax memberAccess
					? memberAccess.Expression.Span
					: invocation.Expression.Span;
				AddTypeDependency(invocation, span, staticContainer, DependencySites.StaticMember, semanticModel, config, paletteSlots, indicators, cancellationToken);
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
			AddTypeDependency(invocation, argument.Span, semanticModel.GetTypeInfo(argument, cancellationToken).Type, DependencySites.GenericInvocation, semanticModel, config, paletteSlots, indicators, cancellationToken);
		}
	}

	private static void AddAttributeDependency(AttributeSyntax attribute, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor)
		{
			AddTypeDependency(attribute, attribute.Name.Span, constructor.ContainingType, DependencySites.Attribute, semanticModel, config, paletteSlots, indicators, cancellationToken);
		}
	}

	private static void AddStaticMemberDependency(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
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
			AddTypeDependency(memberAccess, memberAccess.Expression.Span, containingType, DependencySites.StaticMember, semanticModel, config, paletteSlots, indicators, cancellationToken);
		}
	}

	private static void AddTypeDependency(SyntaxNode callerNode, TextSpan span, ITypeSymbol? dependencyType, string site, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		if (dependencyType is null || TryGetCaller(callerNode, semanticModel, config, cancellationToken) is not { } caller)
		{
			return;
		}

		var seenDependencyNames = new HashSet<string>(StringComparer.Ordinal);
		var index = 0;
		var matchedAnyLayer = false;
		var outerTypeIsIgnored = false;
		var unrecognizedGenericArguments = new List<ITypeSymbol>();

		foreach (var current in EnumerateTypeAndGenericArguments(dependencyType))
		{
			var isOuter = index++ == 0;
			var effectiveSite = isOuter ? site : DependencySites.GenericArgument;
			if (string.IsNullOrEmpty(current.Name))
			{
				continue;
			}

			if (current.Name == caller.TypeName || IsIgnoredRecognitionType(current))
			{
				outerTypeIsIgnored |= isOuter;
				continue;
			}

			var dependencyMatch = config.FindLayer(current.Name, current.ContainingNamespace?.ToDisplayString() ?? string.Empty, current);
			if (dependencyMatch is null)
			{
				if (!isOuter)
				{
					unrecognizedGenericArguments.Add(current);
				}
				continue;
			}

			matchedAnyLayer = true;
			if (!seenDependencyNames.Add(current.Name))
			{
				continue;
			}

			indicators.Add(CreateRecognizedSiteIndicator(span, effectiveSite, caller, current, dependencyMatch.Value, config, paletteSlots));
		}

		if (!matchedAnyLayer && !outerTypeIsIgnored && config.RequiresRecognizedDependencyAt(caller.Match, site))
		{
			indicators.Add(CreateUnrecognizedSiteIndicator(span, site, caller, dependencyType.Name, ArchitectureDependencySiteStatus.Unrecognized, ArchitecturalDiagnosticIds.UnrecognizedDependency, "not assigned to any architectural layer"));
		}

		if (!config.RequiresRecognizedDependencyAt(caller.Match, DependencySites.GenericArgument))
		{
			if (!matchedAnyLayer && !outerTypeIsIgnored && !config.RequiresRecognizedDependencyAt(caller.Match, site) && !string.IsNullOrEmpty(dependencyType.Name))
			{
				indicators.Add(CreateUnrecognizedSiteIndicator(span, site, caller, dependencyType.Name, ArchitectureDependencySiteStatus.Unclassified, null, "not assigned to any architectural layer"));
			}
			return;
		}

		foreach (var argument in unrecognizedGenericArguments)
		{
			if (seenDependencyNames.Add(argument.Name))
			{
				indicators.Add(CreateUnrecognizedSiteIndicator(span, DependencySites.GenericArgument, caller, argument.Name, ArchitectureDependencySiteStatus.Unrecognized, ArchitecturalDiagnosticIds.UnrecognizedDependency, "generic argument is not assigned to any architectural layer"));
			}
		}
	}

	private static ArchitectureDependencySiteIndicator CreateRecognizedSiteIndicator(TextSpan span, string site, CallerInfo caller, ITypeSymbol dependencyType, LayerMatch dependencyMatch, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots)
	{
		var dependencyLayer = dependencyMatch.Layer;
		if (dependencyLayer.IsForbidden)
		{
			var forbiddenReason = dependencyLayer.Comment is null ? "the type matches a global <Forbidden> rule" : "the type matches a global <Forbidden> rule: " + dependencyLayer.Comment;
			var result = CreateRecognizedSiteIndicator(span, site, caller, dependencyType.Name, dependencyLayer.Name, GetPaletteSlot(paletteSlots, dependencyLayer.Name), ArchitectureDependencySiteStatus.TypePolicyViolation, ArchitecturalDiagnosticIds.ForbiddenDependency, forbiddenReason);

			return result;
		}

		var typePolicyViolation = config.EvaluateTypePolicy(dependencyMatch, dependencyType.Name, dependencyType.ContainingNamespace?.ToDisplayString() ?? string.Empty, dependencyType);
		if (typePolicyViolation is { } policyViolation)
		{
			var result = CreateRecognizedSiteIndicator(span, site, caller, dependencyType.Name, policyViolation.DependencyLayerName, GetPaletteSlot(paletteSlots, policyViolation.DependencyLayerName), ArchitectureDependencySiteStatus.TypePolicyViolation, ArchitecturalDiagnosticIds.ForbiddenDependency, policyViolation.Reason);

			return result;
		}

		var edgeEvaluation = config.Graph.EvaluateDependency(caller.Match, dependencyMatch, site);
		if (edgeEvaluation.IsAllowed)
		{
			var result = CreateRecognizedSiteIndicator(span, site, caller, dependencyType.Name, dependencyLayer.Name, GetPaletteSlot(paletteSlots, dependencyLayer.Name), ArchitectureDependencySiteStatus.Allowed, null, "allowed by configured dependency rules");

			return result;
		}

		var status = GetDeniedStatus(caller.LayerPath, dependencyLayer.Name, edgeEvaluation, config);
		var diagnosticId = status switch
		{
			ArchitectureDependencySiteStatus.WrongDirection => ArchitecturalDiagnosticIds.WrongDirectionDependency,
			ArchitectureDependencySiteStatus.SameLayer => ArchitecturalDiagnosticIds.SameLayerDependency,
			_ => ArchitecturalDiagnosticIds.IllegalLevelDependency
		};
		var reason = status == ArchitectureDependencySiteStatus.SameLayer && !edgeEvaluation.IsDeniedBySiteFilter
			? $"types in the same layer ('{caller.LayerPath}') may not depend on each other"
			: status == ArchitectureDependencySiteStatus.WrongDirection && !edgeEvaluation.IsDeniedBySiteFilter
				? $"this dependency goes the wrong direction - the reverse ('{dependencyLayer.Name}' -> '{caller.LayerPath}') is configured"
				: edgeEvaluation.DenialReason;

		var denied = CreateRecognizedSiteIndicator(span, site, caller, dependencyType.Name, dependencyLayer.Name, GetPaletteSlot(paletteSlots, dependencyLayer.Name), status, diagnosticId, reason);

		return denied;
	}

	private static int GetPaletteSlot(ImmutableDictionary<string, int> paletteSlots, string layerPath)
	{
		var result = paletteSlots.TryGetValue(layerPath, out var slot) ? slot : 1;

		return result;
	}

	private static ArchitectureDependencySiteStatus GetDeniedStatus(string callerLayerName, string dependencyLayerName, DependencyEdgeEvaluation edgeEvaluation, ProjectAnalyzerConfig config)
	{
		if (callerLayerName == dependencyLayerName)
		{
			return ArchitectureDependencySiteStatus.SameLayer;
		}

		if (edgeEvaluation.IsDeniedByBlockedEdge)
		{
			return ArchitectureDependencySiteStatus.Blocked;
		}

		if (config.Graph.HasEdge(edgeEvaluation.ScopePath, dependencyLayerName, callerLayerName))
		{
			return ArchitectureDependencySiteStatus.WrongDirection;
		}

		var result = edgeEvaluation.IsDeniedBySiteFilter
			? ArchitectureDependencySiteStatus.SiteFiltered
			: ArchitectureDependencySiteStatus.MissingAllowedDependency;

		return result;
	}

	private static ArchitectureDependencySiteIndicator CreateRecognizedSiteIndicator(TextSpan span, string site, CallerInfo caller, string dependencyTypeName, string dependencyLayerName, int dependencyLayerPaletteSlot, ArchitectureDependencySiteStatus status, string? diagnosticId, string reason)
	{
		var tooltip = $"{site}: {caller.TypeName} ({caller.LayerPath}) -> {dependencyTypeName} ({dependencyLayerName}) - {reason}";
		var result = new ArchitectureDependencySiteIndicator(span, site, caller.TypeName, caller.LayerPath, dependencyTypeName, dependencyLayerName, dependencyLayerPaletteSlot, status, diagnosticId, tooltip, reason);

		return result;
	}

	private static ArchitectureDependencySiteIndicator CreateUnrecognizedSiteIndicator(TextSpan span, string site, CallerInfo caller, string dependencyTypeName, ArchitectureDependencySiteStatus status, string? diagnosticId, string reason)
	{
		var tooltip = $"{site}: {caller.TypeName} ({caller.LayerPath}) -> {dependencyTypeName} - {reason}";
		var result = new ArchitectureDependencySiteIndicator(span, site, caller.TypeName, caller.LayerPath, dependencyTypeName, null, 0, status, diagnosticId, tooltip, reason);

		return result;
	}

	private static CallerInfo? TryGetCaller(SyntaxNode node, SemanticModel semanticModel, ProjectAnalyzerConfig config, CancellationToken cancellationToken)
	{
		var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (typeDeclaration is null || semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not ITypeSymbol callerSymbol)
		{
			return null;
		}

		var callerName = callerSymbol.Name;
		var match = config.FindLayer(callerName, callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, callerSymbol);
		if (match is null || match.Value.Layer.IsForbidden)
		{
			return null;
		}

		var result = new CallerInfo(callerName, match.Value.Layer.Name, match.Value);

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

	private static bool IsIgnoredRecognitionType(ITypeSymbol type)
	{
		var result = type.SpecialType != SpecialType.None
		             || type.TypeKind is TypeKind.TypeParameter or TypeKind.Dynamic or TypeKind.Error;

		return result;
	}
}
