using System.Globalization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

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

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!TryReadRuleLocation(diagnostic, out var line, out var column, out var depTypeName, out var configPath)
		    || depTypeName is null)
		{
			return;
		}

		var discoveredSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(
			context.Document,
			context.CancellationToken).ConfigureAwait(false);
		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(
			context.Document,
			context.CancellationToken).ConfigureAwait(false);
		var configurationSource = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, configPath, snapshots);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		var configFileName = string.IsNullOrWhiteSpace(configurationSource.Path)
			? ConfigFileName
			: Path.GetFileName(configurationSource.Path);
		var requiresReview = await RequiresExceptionReviewAsync(context.Document.Project, configurationSource, context.CancellationToken).ConfigureAwait(false);
		var title = requiresReview
			? $"Add temporary exception requiring review in {configFileName}"
			: $"Add '{depTypeName}' to exceptions in {configFileName}";

		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddException(document, line, column, depTypeName),
					cancellationToken),
				title),
			diagnostic);
	}

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

	private static async Task<bool> RequiresExceptionReviewAsync(Project project, ArchitectureConfigurationSource source, CancellationToken cancellationToken)
	{
		if (source.Kind == ArchitectureConfigurationSourceKind.XmlFile)
		{
			var configDocument = FindConfigDocument(project, source.Path);
			if (configDocument is null)
			{
				return false;
			}

			var configText = await configDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
			var result = RequiresExceptionReview(configText);

			return result;
		}

		if (source.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata)
		{
			var sourceDocument = project.Documents.FirstOrDefault(document =>
				string.Equals(document.FilePath, source.Path, StringComparison.OrdinalIgnoreCase));
			if (sourceDocument is null)
			{
				return false;
			}

			var sourceText = await sourceDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
			var sourceValue = sourceText.ToString();
			if (!RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence.InlineAssemblyMetadataSettings.TryFindInlineSettings(sourceValue, out var settings, out _))
			{
				return false;
			}

			var result = RequiresExceptionReview(Microsoft.CodeAnalysis.Text.SourceText.From(settings.Xml));

			return result;
		}

		return false;
	}

	private static bool TryAddException(XDocument document, int line, int column, string depTypeName)
	{
		var ruleElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document.Descendants(), line, column);
		if (ruleElement is null)
		{
			return false;
		}

		var exceptions = ruleElement.Element("Exceptions");
		if (exceptions is null)
		{
			exceptions = new XElement("Exceptions");
			ruleElement.Add(exceptions);
		}
		else if (exceptions.Elements("Class")
			         .Any(element => string.Equals(element.Attribute("typeName")?.Value, depTypeName, StringComparison.Ordinal)))
		{
			return false;
		}

		var policy = ReadExceptionPolicy(document);
		exceptions.Add(CreateExceptionElement(depTypeName, policy));

		return true;
	}
}
