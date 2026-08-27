namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

public static partial class InlineAssemblyMetadataSettings
{
	public static bool TryCreateInlineSettingsLiteral(InlineSettingsLiteral settings, string updatedXml, string newLine, out string literal, out string message)
	{
		if (!settings.IsInterpolated)
		{
			literal = CreateRawStringLiteral(updatedXml, newLine);
			message = string.Empty;
			return true;
		}

		if (!TryRestoreInterpolatedXml(settings, updatedXml, out var interpolatedXml, out message))
		{
			literal = string.Empty;
			return false;
		}

		literal = "$" + CreateRawStringLiteral(interpolatedXml, newLine);
		message = string.Empty;
		return true;
	}

	private static string CreateRawStringLiteral(string xml, string newLine)
	{
		var delimiter = new string('"', Math.Max(3, GetLongestQuoteRun(xml) + 1));
		var result = delimiter + newLine + xml + newLine + delimiter;

		return result;
	}

	private static int GetLongestQuoteRun(string value)
	{
		var longest = 0;
		var current = 0;
		foreach (var character in value)
		{
			if (character == '"')
			{
				current++;
				longest = Math.Max(longest, current);
			}
			else
			{
				current = 0;
			}
		}

		return longest;
	}
}
