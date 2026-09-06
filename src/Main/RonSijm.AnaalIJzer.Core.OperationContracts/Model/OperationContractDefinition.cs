using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.OperationContracts.Model;

/// <summary>One user-declared operation, its owner, entry points, and optional request/response shapes.</summary>
public readonly struct OperationContractDefinition(
	string name,
	OperationContractDeclarationSelector owner,
	ImmutableArray<OperationContractDeclarationSelector> entryPoints,
	PatternMatcher? requestMatcher,
	PatternMatcher? responseMatcher,
	ImmutableHashSet<string>? allowedOwnerLayers,
	ImmutableHashSet<string>? allowedEntryPointLayers,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string Name { get; } = name;

	public OperationContractDeclarationSelector Owner { get; } = owner;

	public ImmutableArray<OperationContractDeclarationSelector> EntryPoints { get; } = entryPoints.IsDefault ? [] : entryPoints;

	public PatternMatcher? RequestMatcher { get; } = requestMatcher;

	public PatternMatcher? ResponseMatcher { get; } = responseMatcher;

	public ImmutableHashSet<string> AllowedOwnerLayers { get; } = allowedOwnerLayers ?? ImmutableHashSet<string>.Empty;

	public ImmutableHashSet<string> AllowedEntryPointLayers { get; } = allowedEntryPointLayers ?? ImmutableHashSet<string>.Empty;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool HasRequestContract => RequestMatcher.HasValue;

	public bool HasResponseContract => ResponseMatcher.HasValue;
}
