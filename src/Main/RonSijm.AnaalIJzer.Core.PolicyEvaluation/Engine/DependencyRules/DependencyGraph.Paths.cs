using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Engine.DependencyRules;

public readonly partial struct DependencyGraph
{
	private static bool EdgeAppliesAtScope(DependencyEdge edge, string scopePath)
	{
		var result = edge.ScopePath == scopePath
		             || edge.AppliesToDescendants && IsAncestorScope(edge.ScopePath, scopePath);

		return result;
	}

	private static bool IsAncestorScope(string ancestorScopePath, string scopePath)
	{
		if (scopePath.Length == 0)
		{
			return false;
		}

		var result = ancestorScopePath.Length == 0
			? true
			: scopePath.StartsWith(ancestorScopePath + "/", StringComparison.Ordinal);

		return result;
	}

	private static bool EdgeMatches(DependencyEdge edge, string from, string to)
	{
		if (edge.IsAllowAny)
		{
			return true;
		}

		if (edge.IsWildcardTarget)
		{
			return PathMatches(to, edge.To);
		}

		if (edge.IsWildcardSource)
		{
			return PathMatches(from, edge.From);
		}

		return PathMatches(from, edge.From) && PathMatches(to, edge.To);
	}

	private static bool PathMatches(string actualPath, string configuredPath)
	{
		var result = actualPath == configuredPath || actualPath.StartsWith(configuredPath + "/", StringComparison.Ordinal);

		return result;
	}

	private static string FormatBoundary(string scopePath)
	{
		var result = string.IsNullOrEmpty(scopePath) ? "the root boundary" : $"boundary '{scopePath}'";

		return result;
	}

	private static ImmutableArray<string> GetGateScopes(string from, string to)
	{
		var fromParts = from.Split('/');
		var toParts = to.Split('/');
		var commonLength = 0;
		while (commonLength < fromParts.Length && commonLength < toParts.Length && fromParts[commonLength] == toParts[commonLength])
		{
			commonLength++;
		}

		if (commonLength == fromParts.Length && commonLength == toParts.Length)
		{
			return [JoinPath(fromParts, fromParts.Length - 1)];
		}

		var scopes = ImmutableArray.CreateBuilder<string>();
		AddScope(scopes, JoinPath(fromParts, commonLength));
		for (var length = commonLength + 1; length < fromParts.Length; length++)
		{
			AddScope(scopes, JoinPath(fromParts, length));
		}

		for (var length = commonLength + 1; length < toParts.Length; length++)
		{
			AddScope(scopes, JoinPath(toParts, length));
		}

		return scopes.ToImmutable();
	}

	private static string JoinPath(string[] parts, int length)
	{
		var result = length <= 0 ? string.Empty : string.Join("/", parts, 0, length);

		return result;
	}

	private static void AddScope(ImmutableArray<string>.Builder scopes, string scope)
	{
		if (!scopes.Contains(scope))
		{
			scopes.Add(scope);
		}
	}
}

