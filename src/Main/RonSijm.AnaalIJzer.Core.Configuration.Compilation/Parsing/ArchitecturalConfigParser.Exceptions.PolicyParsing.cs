using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static bool ParseExceptionPolicyBoolean(XElement element, string path, string attributeName, ImmutableArray<ConfigurationIssue>.Builder issues, bool defaultValue)
	{
		var value = element.Attribute(attributeName)?.Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		var trimmed = value!.Trim();
		if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
		{
			return true;
		}

		if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) || trimmed == "0")
		{
			return false;
		}

		AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"ExceptionPolicy attribute '{attributeName}' must be true, false, 1, or 0.", element, path);
		return defaultValue;
	}

	private static int ParseExceptionPolicyInteger(XElement element, string path, string attributeName, int defaultValue, int minimum, int maximum, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var value = element.Attribute(attributeName)?.Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
		    && result >= minimum
		    && result <= maximum)
		{
			return result;
		}

		AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"ExceptionPolicy attribute '{attributeName}' must be an integer between {minimum} and {maximum}.", element, path);
		return defaultValue;
	}

	private static bool ExceptionPolicyAttributesEqual(XElement left, XElement right)
	{
		var attributes = new[]
		{
			"requireReason",
			"requireOwner",
			"requireExpiresOn",
			"warnBeforeDays",
			"description"
		};

		foreach (var attributeName in attributes)
		{
			if (!string.Equals(left.Attribute(attributeName)?.Value, right.Attribute(attributeName)?.Value, StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}
}

