using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.GraphModel.Loading;

public static partial class ArchitectureGraphXmlSnapshotLoader
{
	private static (string Status, string Message)? EvaluateExceptionStatus(XElement matcher, LocalExceptionPolicy policy)
	{
		var matcherKind = matcher.Name.LocalName;
		var matcherLabel = FormatMatcherLabel(matcher);
		var reason = matcher.Attribute("reason")?.Value.Trim();
		var owner = matcher.Attribute("owner")?.Value.Trim();
		var expiresOnText = matcher.Attribute("expiresOn")?.Value.Trim();
		if (policy.RequireReason && string.IsNullOrWhiteSpace(reason))
		{
			return ("Invalid", $"Architecture exception for {matcherKind} '{matcherLabel}' is missing required reason metadata");
		}

		if (policy.RequireOwner && string.IsNullOrWhiteSpace(owner))
		{
			return ("Invalid", $"Architecture exception for {matcherKind} '{matcherLabel}' is missing required owner metadata");
		}

		if (policy.RequireExpiresOn && string.IsNullOrWhiteSpace(expiresOnText))
		{
			return ("Invalid", $"Architecture exception for {matcherKind} '{matcherLabel}' is missing required expiresOn metadata");
		}

		if (string.IsNullOrWhiteSpace(expiresOnText))
		{
			return null;
		}

		if (!DateTime.TryParseExact(expiresOnText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiresOn))
		{
			return ("Invalid", $"Architecture exception for {matcherKind} '{matcherLabel}' has an invalid expiresOn date '{expiresOnText}'");
		}

		var today = DateTime.Today;
		if (expiresOn.Date < today)
		{
			return ("Expired", $"Architecture exception for {matcherKind} '{matcherLabel}' has expired on {expiresOn:yyyy-MM-dd} and is no longer applied");
		}

		var daysUntilExpiry = (expiresOn.Date - today).Days;
		if (daysUntilExpiry <= policy.WarnBeforeDays)
		{
			return ("ExpiringSoon", $"Architecture exception for {matcherKind} '{matcherLabel}' expires in {daysUntilExpiry} day{(daysUntilExpiry == 1 ? string.Empty : "s")} on {expiresOn:yyyy-MM-dd}");
		}

		return null;
	}

	private static LocalExceptionPolicy ReadExceptionPolicy(ImmutableArray<ConfigurationDocumentPart> documents)
	{
		foreach (var document in documents)
		{
			var element = document.Root.Element(ExceptionPolicyElementName);
			if (element is null)
			{
				continue;
			}

			var result = new LocalExceptionPolicy(
				ReadBooleanAttribute(element, RequireExceptionReasonAttributeName),
				ReadBooleanAttribute(element, RequireExceptionOwnerAttributeName),
				ReadBooleanAttribute(element, RequireExceptionExpiresOnAttributeName),
				ReadIntegerAttribute(element, ExceptionWarnBeforeDaysAttributeName, 14));

			return result;
		}

		return new LocalExceptionPolicy(false, false, false, 14);
	}

	private static bool ReadBooleanAttribute(XElement element, string attributeName)
	{
		var value = element.Attribute(attributeName)?.Value;
		var result = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	private static int ReadIntegerAttribute(XElement element, string attributeName, int defaultValue)
	{
		var value = element.Attribute(attributeName)?.Value;
		if (int.TryParse(value, out var parsed))
		{
			return parsed;
		}

		var result = defaultValue;

		return result;
	}

	private readonly struct LocalExceptionPolicy(bool requireReason, bool requireOwner, bool requireExpiresOn, int warnBeforeDays)
	{
		public bool RequireReason { get; } = requireReason;

		public bool RequireOwner { get; } = requireOwner;

		public bool RequireExpiresOn { get; } = requireExpiresOn;

		public int WarnBeforeDays { get; } = warnBeforeDays;
	}
}
