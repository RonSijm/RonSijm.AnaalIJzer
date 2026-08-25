using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Engine.ApiSurface;

using RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;

namespace RonSijm.AnaalIJzer.Analysis.ApiSurface.Traversal;

internal static class TransitiveExposureWalker
{
	internal static TransitiveExposureViolationCandidate? FindFirstViolation(
		INamedTypeSymbol rootType,
		string rootMember,
		int maxDepth,
		ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>> memberCache,
		Func<INamedTypeSymbol, string, int, (ApiSurfaceEvaluation? Evaluation, string? LayerName)> evaluateCandidate,
		CancellationToken cancellationToken)
	{
		var queue = new Queue<TraversalItem>();
		var visitedDepths = new Dictionary<INamedTypeSymbol, int>(SymbolEqualityComparer.Default);
		queue.Enqueue(new TraversalItem(rootType, 0, new ExposurePath(rootMember, ImmutableArray<ExposurePathSegment>.Empty)));
		visitedDepths[rootType.OriginalDefinition] = 0;

		while (queue.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var current = queue.Dequeue();
			if (current.Depth >= maxDepth)
			{
				continue;
			}

			var references = memberCache.GetOrAdd(current.Type.OriginalDefinition, type => ExternallyVisibleMemberEnumerator.GetReferences(type, cancellationToken));
			foreach (var reference in references)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var nextDepth = current.Depth + 1;
				foreach (var (candidateType, segmentName) in ExternallyVisibleMemberEnumerator.ExpandNamedTypes(reference))
				{
					var canonicalType = candidateType.OriginalDefinition;
					var (evaluation, dependencyLayerName) = evaluateCandidate(canonicalType, reference.Site, nextDepth);
					var segment = new ExposurePathSegment(segmentName, reference.Location);
					var path = current.Path.Append(segment);
					if (evaluation is not null)
					{
						var nestedMember = FindNestedMember(current.Type, reference.SegmentName);
						var result = new TransitiveExposureViolationCandidate(
							canonicalType,
							dependencyLayerName,
							evaluation.Value,
							reference.Site,
							path,
							nextDepth,
							nestedMember,
							reference.Location);

						return result;
					}

					if (nextDepth >= maxDepth || !CanTraverse(canonicalType, dependencyLayerName))
					{
						continue;
					}

					if (visitedDepths.TryGetValue(canonicalType, out var visitedDepth) && visitedDepth <= nextDepth)
					{
						continue;
					}

					visitedDepths[canonicalType] = nextDepth;
					queue.Enqueue(new TraversalItem(canonicalType, nextDepth, path));
				}
			}
		}

		return null;
	}

	private static bool CanTraverse(INamedTypeSymbol type, string? dependencyLayerName)
	{
		var result = dependencyLayerName is not null
		             && type.SpecialType == SpecialType.None
		             && type.TypeKind != TypeKind.Enum
		             && !type.IsUnboundGenericType;

		return result;
	}

	private static ISymbol? FindNestedMember(INamedTypeSymbol containingType, string segmentName)
	{
		var marker = containingType.Name + ".";
		var memberName = segmentName.StartsWith(marker, StringComparison.Ordinal)
			? segmentName.Substring(marker.Length).Split('(', '[', '.', '*')[0]
			: string.Empty;
		var result = memberName.Length == 0 ? null : containingType.GetMembers(memberName).FirstOrDefault();

		return result;
	}

	private readonly struct TraversalItem(INamedTypeSymbol type, int depth, ExposurePath path)
	{
		public INamedTypeSymbol Type { get; } = type;
		public int Depth { get; } = depth;
		public ExposurePath Path { get; } = path;
	}
}
