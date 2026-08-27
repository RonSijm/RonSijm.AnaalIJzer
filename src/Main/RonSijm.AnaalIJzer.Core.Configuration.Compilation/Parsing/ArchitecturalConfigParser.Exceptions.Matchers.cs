using System.Globalization;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Exceptions;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ArchitectureExceptionMetadata ParseExceptionMetadata(XElement element)
	{
		var expiresOnText = element.Attribute("expiresOn")?.Value;
		DateTime? expiresOn = null;
		if (!string.IsNullOrWhiteSpace(expiresOnText)
		    && DateTime.TryParseExact(expiresOnText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedExpiresOn))
		{
			expiresOn = parsedExpiresOn.Date;
		}

		var result = new ArchitectureExceptionMetadata(
			element.Attribute("reason")?.Value,
			element.Attribute("owner")?.Value,
			expiresOnText,
			expiresOn);

		return result;
	}
}

