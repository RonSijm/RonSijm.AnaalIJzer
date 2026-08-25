using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
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
}
