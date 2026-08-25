using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
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

			var dependencyMatch = config.Engine.FindLayer(current.Name, current.ContainingNamespace?.ToDisplayString() ?? string.Empty, current);
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
		var decision = DependencyRuleEvaluator.Evaluate(config, caller.Match, dependencyMatch, dependencyType, site);
		var denied = CreateRecognizedSiteIndicator(
			span,
			site,
			caller,
			dependencyType.Name,
			decision.DependencyLayerName,
			GetPaletteSlot(paletteSlots, decision.DependencyLayerName),
			decision.Status,
			decision.DiagnosticId,
			decision.Reason);

		return denied;
	}

	private static int GetPaletteSlot(ImmutableDictionary<string, int> paletteSlots, string layerPath)
	{
		var result = paletteSlots.TryGetValue(layerPath, out var slot) ? slot : 1;

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
		var match = config.Engine.FindLayer(callerName, callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, callerSymbol);
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
