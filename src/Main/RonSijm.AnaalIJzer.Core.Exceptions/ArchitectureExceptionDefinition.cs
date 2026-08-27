using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.Exceptions;

public sealed class ArchitectureExceptionDefinition(
	string matcherKind,
	string matcherLabel,
	PatternMatcher matcher,
	ArchitectureExceptionMetadata metadata,
	ImmutableArray<ArchitectureExceptionDefinition> nested,
	string ownerLayerPath,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition,
	ArchitectureExceptionStatus status)
{
	public string MatcherKind { get; } = matcherKind;

	public string MatcherLabel { get; } = matcherLabel;

	public PatternMatcher Matcher { get; } = matcher;

	public ArchitectureExceptionMetadata Metadata { get; } = metadata;

	public ImmutableArray<ArchitectureExceptionDefinition> Nested { get; } = nested;

	public string OwnerLayerPath { get; } = ownerLayerPath;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public ArchitectureExceptionStatus Status { get; } = status;

	public bool IsActive
	{
		get
		{
			var result = Status is ArchitectureExceptionStatus.Active or ArchitectureExceptionStatus.ExpiringSoon;

			return result;
		}
	}
}
