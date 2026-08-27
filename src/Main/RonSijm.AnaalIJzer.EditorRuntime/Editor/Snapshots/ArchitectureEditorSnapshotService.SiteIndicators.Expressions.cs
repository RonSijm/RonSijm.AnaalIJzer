using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
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
}
