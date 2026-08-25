using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static partial class AddToExceptionsCodeFix
{
	/// <summary>
	///     Produces the updated XML text after inserting <paramref name="depTypeName" /> as an
	///     exception under the matcher element located at
	///     (<paramref name="line" />, <paramref name="column" />) in <paramref name="original" />.
	///     Returns <see langword="null" /> if the element cannot be located or is already excepted.
	/// </summary>
	internal static SourceText? AddException(SourceText original, int line, int column, string depTypeName)
	{
		var content = original.ToString();
		XDocument doc;
		try
		{
			doc = XDocument.Parse(content, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
		}
		catch
		{
			return null;
		}

		var policy = ReadExceptionPolicy(doc);

		var ruleEl = FindElementAt(doc, line, column);
		if (ruleEl is null)
		{
			return null;
		}

		var exceptions = ruleEl.Element("Exceptions");
		if (exceptions is null)
		{
			exceptions = new XElement("Exceptions");
			ruleEl.Add(exceptions);
		}
		else if (HasExistingTypeNameException(exceptions, depTypeName))
		{
			return null;
		}

		exceptions.Add(CreateExceptionElement(depTypeName, policy));

		var sb = new StringBuilder();
		using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
		       {
			       OmitXmlDeclaration = doc.Declaration is null,
			       Indent = false,
			       NewLineHandling = NewLineHandling.None,
		       }))
		{
			doc.Save(writer);
		}

		return SourceText.From(sb.ToString(), original.Encoding ?? Encoding.UTF8);
	}

	private static XElement? FindElementAt(XDocument doc, int line, int column)
	{
		foreach (var el in doc.Descendants())
		{
			var info = (IXmlLineInfo)el;
			if (info.HasLineInfo() && info.LineNumber == line && info.LinePosition == column)
			{
				return el;
			}
		}

		return null;
	}

	private static bool HasExistingTypeNameException(XElement exceptions, string depTypeName)
	{
		foreach (var el in exceptions.Elements("Class"))
		{
			if (string.Equals(el.Attribute("typeName")?.Value, depTypeName, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}
}
