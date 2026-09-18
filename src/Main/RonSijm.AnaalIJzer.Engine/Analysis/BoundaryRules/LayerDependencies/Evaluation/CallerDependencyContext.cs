using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

internal readonly struct CallerDependencyContext(string typeName, string namespaceName, ITypeSymbol symbol, LayerMatch? layerMatch)
{
	public string TypeName { get; } = typeName;

	public string NamespaceName { get; } = namespaceName;

	public ITypeSymbol Symbol { get; } = symbol;

	public LayerMatch? LayerMatch { get; } = layerMatch;
}
