using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Parsing;

namespace RonSijm.AnaalIJzer.Diagnostics;

/// <summary>
///     Shared logic for the "Add '<c>TypeName</c>' to exceptions" code action.
///     Locates the originating <c>&lt;Class&gt;</c>, <c>&lt;Namespace&gt;</c>, or <c>&lt;Assembly&gt;</c> element by the
///     line/column carried in the diagnostic properties and appends (or extends) an
///     <c>&lt;Exceptions&gt;</c> child with a new <c>&lt;Class typeName="…" /&gt;</c> entry.
/// </summary>
internal static class AddToExceptionsCodeFix
{
	internal const string ConfigFileName = ArchitecturalConfigParser.ConfigFileName;

	/// <summary>
	///     Finds the additional document for <c>Architecture.anl</c> in
	///     <paramref name="project" />. Returns <see langword="null" /> if not present.
	/// </summary>
	internal static TextDocument? FindConfigDocument(Project project, string? configPath = null)
	{
		TextDocument? fileNameMatch = null;
		var expectedFileName = string.IsNullOrWhiteSpace(configPath)
			? ArchitecturalConfigParser.ConfigFileName
			: Path.GetFileName(configPath!) ?? string.Empty;

		foreach (var doc in project.AdditionalDocuments)
		{
			if (doc.FilePath is not { } path)
			{
				continue;
			}

			if (configPath is { Length: > 0 } && !string.IsNullOrWhiteSpace(configPath) && IsSamePath(path, configPath))
			{
				return doc;
			}

			if (string.Equals(Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase))
			{
				fileNameMatch ??= doc;
			}
		}

		return fileNameMatch;
	}

	internal static bool TryReadRuleLocation(Diagnostic diagnostic, out int line, out int column, out string? depTypeName)
	{
		var result = TryReadRuleLocation(diagnostic, out line, out column, out depTypeName, out _);

		return result;
	}

    internal static bool TryReadRuleLocation(Diagnostic diagnostic, out int line, out int column, out string? depTypeName, out string? configPath)
	{
		line = 0;
		column = 0;
		depTypeName = null;
		configPath = null;

		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyDepTypeName, out depTypeName)
		    || string.IsNullOrEmpty(depTypeName))
		{
			return false;
		}

		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyRuleXmlLine, out var lineText)
		    || !int.TryParse(lineText, NumberStyles.Integer, CultureInfo.InvariantCulture, out line)
		    || line <= 0)
		{
			return false;
		}

		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyRuleXmlCol, out var colText)
		    || !int.TryParse(colText, NumberStyles.Integer, CultureInfo.InvariantCulture, out column)
		    || column <= 0)
		{
			return false;
		}

		diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyRuleXmlPath, out configPath);
		return true;
	}

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

		exceptions.Add(new XElement("Class", new XAttribute("typeName", depTypeName)));

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

	private static bool IsSamePath(string left, string right)
	{
		try
		{
			return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
		}
	}
}
