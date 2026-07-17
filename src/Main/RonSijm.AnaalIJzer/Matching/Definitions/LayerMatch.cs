using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Definitions;

/// <summary>The result of a <see cref="AnalyzerConfig.FindLayer" /> call.</summary>
internal readonly struct LayerMatch(LayerDefinition layer, ImmutableArray<LayerDefinition> layers, ImmutableArray<LayerMatcherMatch> matcherMatches, string? matchedSuffix, int xmlLineNumber, int xmlLinePosition, string xmlPath)
{
	public LayerDefinition Layer { get; } = layer;

	/// <summary>The complete outer-to-inner boundary path for the matched type.</summary>
	public ImmutableArray<LayerDefinition> Layers { get; } = layers;

	/// <summary>Every matcher that established the outer-to-inner layer path.</summary>
	public ImmutableArray<LayerMatcherMatch> MatcherMatches { get; } = matcherMatches;

	/// <summary>
	///     The suffix that triggered the match (for code-fix rename), or <see langword="null" />
	///     when matched via exact <c>typeName=</c>, <c>startsWith=</c>, <c>contains=</c>,
	///     or a <c>Namespace</c> rule.
	/// </summary>
	public string? MatchedSuffix { get; } = matchedSuffix;

	/// <summary>
	///     1-based line number of the originating <c>&lt;Class&gt;</c> or <c>&lt;Namespace&gt;</c>
	///     element in <c>Architecture.anl</c>. Used by the "Add to exceptions" code fix
	///     to disambiguate when multiple matchers could apply to the same dependency type.
	/// </summary>
	public int XmlLineNumber { get; } = xmlLineNumber;

	/// <summary>
	///     1-based column position of the originating XML element. See <see cref="XmlLineNumber" />.
	/// </summary>
	public int XmlLinePosition { get; } = xmlLinePosition;

	public string XmlPath { get; } = xmlPath;
}

internal readonly struct LayerMatcherMatch(LayerDefinition layer, int xmlLineNumber, int xmlLinePosition, string xmlPath)
{
	public LayerDefinition Layer { get; } = layer;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;
	public string XmlPath { get; } = xmlPath;
}
