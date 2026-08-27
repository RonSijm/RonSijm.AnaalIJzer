using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.GraphModel.Loading;

public static partial class ArchitectureGraphXmlSnapshotLoader
{
	private static string FormatMatcherLabel(XElement matcher)
	{
		var displayAttributes = matcher.Attributes()
			.Where(attribute => attribute.Name.LocalName is not "reason" and not "owner" and not "expiresOn" and not "description")
			.Select(attribute => attribute.Name.LocalName + "=\"" + attribute.Value + "\"")
			.ToArray();
		var result = displayAttributes.Length == 0
			? matcher.Name.LocalName
			: string.Join(" ", displayAttributes);

		return result;
	}
}
