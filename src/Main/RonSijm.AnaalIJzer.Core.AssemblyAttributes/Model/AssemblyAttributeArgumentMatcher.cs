using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.AssemblyAttributes.Formatting;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;

/// <summary>Matches either one positional or one named assembly-attribute argument.</summary>
public readonly struct AssemblyAttributeArgumentMatcher(int? index, string? name, PatternMatcher matcher)
{
	public int? Index { get; } = index;

	public string? Name { get; } = name;

	public PatternMatcher Matcher { get; } = matcher;

	public bool Matches(AttributeData attribute)
	{
		if (Index is { } index)
		{
			if (index < 0 || index >= attribute.ConstructorArguments.Length)
			{
				return false;
			}

			var positionalResult = Matches(attribute.ConstructorArguments[index]);

			return positionalResult;
		}

		if (string.IsNullOrWhiteSpace(Name))
		{
			return false;
		}

		foreach (var namedArgument in attribute.NamedArguments)
		{
			if (!string.Equals(namedArgument.Key, Name, StringComparison.Ordinal))
			{
				continue;
			}

			var namedResult = Matches(namedArgument.Value);

			return namedResult;
		}

		return false;
	}

	private bool Matches(TypedConstant value)
	{
		var text = AssemblyAttributeArgumentValueFormatter.Format(value);
		var result = Matcher.TryMatch(text, string.Empty) is not null;

		return result;
	}
}
