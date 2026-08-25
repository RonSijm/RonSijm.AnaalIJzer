using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;

namespace RonSijm.AnaalIJzer.Engine.EntryPoints;

public readonly struct BoundaryEntryPointSelector(string? layerPath, ImmutableArray<BoundaryEntryPointMatcher> matchers)
{
	public string? LayerPath { get; } = layerPath;

	public ImmutableArray<BoundaryEntryPointMatcher> Matchers { get; } = matchers;

	public bool IsLayerSelector
	{
		get { return LayerPath is not null; }
	}

	public bool Matches(string dependencyLayerPath, string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType)
	{
		if (LayerPath is { } selectorLayerPath)
		{
			var result = IsContainedInBoundary(selectorLayerPath, dependencyLayerPath);

			return result;
		}

		foreach (var matcher in Matchers)
		{
			if (matcher.Matches(dependencyTypeName, dependencyNamespace, dependencyType))
			{
				return true;
			}
		}

		return false;
	}

	public string ToDisplayText()
	{
		if (LayerPath is { } selectorLayerPath)
		{
			var result = selectorLayerPath;

			return result;
		}

		var result2 = string.Join(" or ", Matchers.Select(matcher => matcher.ToDisplayText()));

		return result2;
	}

	public static bool IsContainedInBoundary(string boundaryPath, string candidatePath)
	{
		var result = candidatePath == boundaryPath || candidatePath.StartsWith(boundaryPath + "/", StringComparison.Ordinal);

		return result;
	}
}

public readonly struct BoundaryEntryPointMatcher(PatternMatcher matcher, ImmutableArray<ExceptionMatcher> exceptions, string displayText)
{
	public PatternMatcher Matcher { get; } = matcher;

	public ImmutableArray<ExceptionMatcher> Exceptions { get; } = exceptions;

	public string DisplayText { get; } = displayText;

	public bool Matches(string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType)
	{
		var result = Matcher.TryMatch(dependencyTypeName, dependencyNamespace, dependencyType) is not null
		             && !IsExcepted(Exceptions, dependencyTypeName, dependencyNamespace, dependencyType);

		return result;
	}

	public string ToDisplayText()
	{
		var result = DisplayText;

		return result;
	}

	private static bool IsExcepted(ImmutableArray<ExceptionMatcher> exceptions, string typeName, string namespaceName, ITypeSymbol symbol)
	{
		if (exceptions.IsDefaultOrEmpty)
		{
			return false;
		}

		var deepestMatchingDepth = 0;
		foreach (var exception in exceptions)
		{
			var depth = exception.FindDeepestMatchingDepth(typeName, namespaceName, symbol, 1);
			if (depth > deepestMatchingDepth)
			{
				deepestMatchingDepth = depth;
			}
		}

		var result = deepestMatchingDepth % 2 == 1;

		return result;
	}
}
