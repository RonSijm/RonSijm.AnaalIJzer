using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Analysis.ApiSurface;

internal static partial class ApiSurfaceAnalyzer
{
	private readonly struct ApiSurfaceDiagnosticKey(SyntaxTree? syntaxTree, TextSpan span, INamedTypeSymbol dependencyType, string site) : IEquatable<ApiSurfaceDiagnosticKey>
	{
		private SyntaxTree? SyntaxTree { get; } = syntaxTree;
		private TextSpan Span { get; } = span;
		private INamedTypeSymbol DependencyType { get; } = dependencyType;
		private string Site { get; } = site;

		public bool Equals(ApiSurfaceDiagnosticKey other)
		{
			var result = ReferenceEquals(SyntaxTree, other.SyntaxTree)
			             && Span.Equals(other.Span)
			             && SymbolEqualityComparer.Default.Equals(DependencyType, other.DependencyType)
			             && string.Equals(Site, other.Site, StringComparison.Ordinal);

			return result;
		}

		public override bool Equals(object? obj)
		{
			var result = obj is ApiSurfaceDiagnosticKey other && Equals(other);

			return result;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = SyntaxTree?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ Span.GetHashCode();
				hashCode = (hashCode * 397) ^ SymbolEqualityComparer.Default.GetHashCode(DependencyType);
				hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Site);

				return hashCode;
			}
		}
	}
}
