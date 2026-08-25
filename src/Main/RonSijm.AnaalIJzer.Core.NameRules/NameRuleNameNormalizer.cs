using System.Text;

namespace RonSijm.AnaalIJzer.Engine.NameRules;

public static class NameRuleNameNormalizer
{
	private static readonly string[] VerbPrefixes = ["Get", "Create", "Build", "Make", "To", "As"];

	public static string Normalize(string name)
	{
		var tokens = new List<string>();
		foreach (var segment in name.Split('.'))
		{
			AddSegmentTokens(tokens, segment);
		}

		var result = string.Join(".", tokens);

		return result;
	}

	private static void AddSegmentTokens(List<string> tokens, string segment)
	{
		var trimmed = TrimSegment(segment);
		if (trimmed.Length == 0 || string.Equals(trimmed, "this", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		foreach (var token in SplitWords(TrimVerbPrefix(trimmed)))
		{
			if (token.Length > 0)
			{
				tokens.Add(token.ToLowerInvariant());
			}
		}
	}

	private static string TrimSegment(string segment)
	{
		var result = segment.Trim().TrimStart('@', '_');

		return result;
	}

	private static string TrimVerbPrefix(string value)
	{
		var result = value;
		foreach (var prefix in VerbPrefixes)
		{
			if (value.Length > prefix.Length && value.StartsWith(prefix, StringComparison.Ordinal) && char.IsUpper(value[prefix.Length]))
			{
				result = value.Substring(prefix.Length);
				break;
			}
		}

		return result;
	}

	private static IEnumerable<string> SplitWords(string value)
	{
		var tokens = new List<string>();
		var current = new StringBuilder();
		for (var i = 0; i < value.Length; i++)
		{
			var character = value[i];
			if (!char.IsLetterOrDigit(character))
			{
				Flush(current);
				continue;
			}

			if (current.Length > 0 && StartsNewWord(value, i))
			{
				Flush(current);
			}

			current.Append(character);
		}

		Flush(current);

		return tokens;

		void Flush(StringBuilder builder)
		{
			if (builder.Length == 0)
			{
				return;
			}

			tokens.Add(builder.ToString());
			builder.Clear();
		}
	}

	private static bool StartsNewWord(string value, int index)
	{
		var current = value[index];
		var previous = value[index - 1];
		var next = index + 1 < value.Length ? value[index + 1] : '\0';
		var result = char.IsUpper(current)
		             && (!char.IsUpper(previous)
		                 || next != '\0' && char.IsLower(next));

		return result;
	}
}
