using System.Globalization;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

namespace RonSijm.AnaalIJzer.Diagnostics;

/// <summary>
///     Shared logic for the "Add '<c>TypeName</c>' to exceptions" code action.
///     Locates the originating <c>&lt;Class&gt;</c>, <c>&lt;Namespace&gt;</c>, or <c>&lt;Assembly&gt;</c> element by the
///     line/column carried in the diagnostic properties and appends (or extends) an
///     <c>&lt;Exceptions&gt;</c> child with a new <c>&lt;Class typeName="…" /&gt;</c> entry.
/// </summary>
internal static partial class AddToExceptionsCodeFix
{
	internal const string ConfigFileName = ArchitectureConfigurationDocumentLoader.ConfigFileName;

	/// <summary>
	///     Finds the additional document for <c>Architecture.anl</c> in
	///     <paramref name="project" />. Returns <see langword="null" /> if not present.
	/// </summary>
	internal static TextDocument? FindConfigDocument(Project project, string? configPath = null)
	{
		TextDocument? fileNameMatch = null;
		var expectedFileName = string.IsNullOrWhiteSpace(configPath)
			? ArchitectureConfigurationDocumentLoader.ConfigFileName
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
}
