using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Matching;

/// <summary>
///     The layer assignment + optional exception list produced by a single
///     <c>&lt;Class&gt;</c> or <c>&lt;Namespace&gt;</c> rule. Carries the originating
///     XML element's <see cref="XmlLineNumber" /> / <see cref="XmlLinePosition" /> so
///     the "Add to exceptions" code fix can locate the element in the additional file.
/// </summary>
internal readonly struct MatcherRule(LayerDefinition layer, ImmutableArray<ExceptionMatcher> exceptions, int xmlLineNumber, int xmlLinePosition, string xmlPath)
{
	public LayerDefinition Layer { get; } = layer;

	/// <summary>
	///     Pattern matchers that, if any of them matches the dependency, cause this
	///     rule to be skipped. <see langword="default" /> means no exceptions.
	/// </summary>
	public ImmutableArray<ExceptionMatcher> Exceptions { get; } = exceptions;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public string XmlPath { get; } = xmlPath;
}
