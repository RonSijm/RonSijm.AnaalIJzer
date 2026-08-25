using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Exceptions;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static partial class AddToExceptionsCodeFix
{
	internal static bool RequiresExceptionReview(SourceText original)
	{
		var content = original.ToString();
		try
		{
			var document = XDocument.Parse(content, LoadOptions.SetLineInfo);
			var policy = ReadExceptionPolicy(document);

			return policy.RequiresMetadata;
		}
		catch
		{
			return false;
		}
	}

	private static ArchitectureExceptionPolicy ReadExceptionPolicy(XDocument document)
	{
		var policyElement = document.Root?.Element("ExceptionPolicy");
		if (policyElement is null)
		{
			return ArchitectureExceptionPolicy.Disabled;
		}

		var requireReason = ReadBooleanAttribute(policyElement, "requireReason");
		var requireOwner = ReadBooleanAttribute(policyElement, "requireOwner");
		var requireExpiresOn = ReadBooleanAttribute(policyElement, "requireExpiresOn");
		var warnBeforeDays = ReadIntegerAttribute(policyElement, "warnBeforeDays", 14);
		var lineInfo = (IXmlLineInfo)policyElement;
		var result = new ArchitectureExceptionPolicy(
			true,
			requireReason,
			requireOwner,
			requireExpiresOn,
			warnBeforeDays,
			policyElement.Attribute("description")?.Value,
			string.Empty,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);

		return result;
	}

	private static XElement CreateExceptionElement(string depTypeName, ArchitectureExceptionPolicy policy)
	{
		var element = new XElement("Class", new XAttribute("typeName", depTypeName));
		if (policy.RequireReason)
		{
			element.Add(new XAttribute("reason", "TODO: explain this exception"));
		}

		if (policy.RequireOwner)
		{
			element.Add(new XAttribute("owner", "TODO: assign an owner"));
		}

		if (policy.RequireExpiresOn)
		{
			var expiresOn = ArchitectureClock.UtcToday.AddDays(30);
			element.Add(new XAttribute("expiresOn", expiresOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
		}

		return element;
	}

	private static bool ReadBooleanAttribute(XElement element, string attributeName)
	{
		var text = element.Attribute(attributeName)?.Value;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		var trimmed = text!.Trim();
		var result = string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || trimmed == "1";

		return result;
	}

	private static int ReadIntegerAttribute(XElement element, string attributeName, int defaultValue)
	{
		var text = element.Attribute(attributeName)?.Value;
		if (string.IsNullOrWhiteSpace(text))
		{
			return defaultValue;
		}

		var result = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: defaultValue;

		return result;
	}
}
