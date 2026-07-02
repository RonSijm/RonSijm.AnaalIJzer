using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Matching;

/// <summary>
///     A single pattern rule that assigns a type to a layer based on its type name,
///     namespace, base type, implemented interfaces, attributes, access modifiers,
///     or a regular-expression match against the simple name / namespace string.
/// </summary>
internal readonly struct PatternMatcher(MatchTarget target, MatchKind kind, string value)
{
	private static readonly ConcurrentDictionary<string, Regex?> RegexCache = new(StringComparer.Ordinal);

	public MatchTarget Target { get; } = target;

	public MatchKind Kind { get; } = kind;

	public string Value { get; } = value;

	/// <summary>
	///     Attempts to match against <paramref name="typeName" /> / <paramref name="namespaceName" />,
	///     and against the semantic <paramref name="symbol" /> when the matcher kind requires it
	///     (<see cref="MatchKind.Inherits" />, <see cref="MatchKind.Implements" />,
	///     <see cref="MatchKind.HasAttribute" />, <see cref="MatchKind.HasAccessModifier" />).
	///     Returns <see langword="null" /> on no match.
	///     Returns the matched suffix string (for code-fix rename) when matched via
	///     <see cref="MatchTarget.TypeName" /> / <see cref="MatchKind.EndsWith" />;
	///     returns <see cref="string.Empty" /> for all other matches.
	/// </summary>
	public string? TryMatch(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var matched = Kind switch
		{
			MatchKind.EndsWith => Subject(typeName, namespaceName, symbol).EndsWith(Value, StringComparison.Ordinal),
			MatchKind.StartsWith => Subject(typeName, namespaceName, symbol).StartsWith(Value, StringComparison.Ordinal),
			MatchKind.Contains => Subject(typeName, namespaceName, symbol).Contains(Value),
			MatchKind.Equals => string.Equals(Subject(typeName, namespaceName, symbol), Value, StringComparison.Ordinal),
			MatchKind.EqualsFullName => string.Equals(FullName(typeName, namespaceName), Value, StringComparison.Ordinal),
			MatchKind.Inherits => symbol is not null && InheritsFrom(symbol, Value),
			MatchKind.Implements => symbol is not null && ImplementsInterface(symbol, Value),
			MatchKind.HasAttribute => symbol is not null && HasAttribute(symbol, Value),
			MatchKind.HasAccessModifier => symbol is not null && HasAccessModifier(symbol, Value),
			MatchKind.Regex => RegexMatches(Subject(typeName, namespaceName, symbol), Value),
			_ => false
		};

		if (!matched)
		{
			return null;
		}

		// Only TypeName.EndsWith can produce a rename-capable suffix for the code fix.
		return Target == MatchTarget.TypeName && Kind == MatchKind.EndsWith
			? Value
			: string.Empty;
	}

	private string Subject(string typeName, string namespaceName, ITypeSymbol? symbol = null) =>
		Target switch
		{
			MatchTarget.Namespace => namespaceName,
			MatchTarget.Assembly => symbol?.ContainingAssembly?.Name ?? string.Empty,
			_ => typeName
		};

	private static string FullName(string typeName, string namespaceName) =>
		string.IsNullOrEmpty(namespaceName) ? typeName : namespaceName + "." + typeName;

	private static bool RegexMatches(string subject, string pattern)
	{
		var regex = RegexCache.GetOrAdd(pattern, static p =>
		{
			try
			{
				return new Regex(p, RegexOptions.CultureInvariant);
			}
			catch (ArgumentException)
			{
				return null;
			}
		});

		return regex is not null && regex.IsMatch(subject);
	}

	private static bool InheritsFrom(ITypeSymbol symbol, string value)
	{
		for (var b = symbol.BaseType; b is not null; b = b.BaseType)
		{
			if (NameMatches(b, value))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ImplementsInterface(ITypeSymbol symbol, string value)
	{
		foreach (var i in symbol.AllInterfaces)
		{
			if (NameMatches(i, value))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasAttribute(ISymbol symbol, string value)
	{
		var normalised = value.EndsWith("Attribute", StringComparison.Ordinal) ? value : value + "Attribute";

		foreach (var attr in symbol.GetAttributes())
		{
			var cls = attr.AttributeClass;
			if (cls is null)
			{
				continue;
			}

			if (string.Equals(cls.Name, normalised, StringComparison.Ordinal)
			    || string.Equals(cls.Name, value, StringComparison.Ordinal)
			    || string.Equals(cls.ToDisplayString(), value, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool HasAccessModifier(ITypeSymbol symbol, string value)
	{
		// Space-separated tokens; all must match (e.g. "public sealed").
		foreach (var token in value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
		{
			if (!MatchesSingleModifier(symbol, token))
			{
				return false;
			}
		}
		return true;
	}

	private static bool MatchesSingleModifier(ITypeSymbol symbol, string token) => token.ToLowerInvariant() switch
	{
		"public" => symbol.DeclaredAccessibility == Accessibility.Public,
		"internal" => symbol.DeclaredAccessibility == Accessibility.Internal,
		"private" => symbol.DeclaredAccessibility == Accessibility.Private,
		"protected" => symbol.DeclaredAccessibility == Accessibility.Protected,
		"sealed" => symbol.IsSealed,
		"abstract" => symbol.IsAbstract,
		"static" => symbol.IsStatic,
		"record" => symbol.IsRecord,
		_ => false
	};

	private static bool NameMatches(INamedTypeSymbol symbol, string value) =>
		string.Equals(symbol.Name, value, StringComparison.Ordinal)
		|| string.Equals(symbol.ToDisplayString(), value, StringComparison.Ordinal);
}
