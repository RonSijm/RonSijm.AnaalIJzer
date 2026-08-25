using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;

namespace RonSijm.AnaalIJzer;

public sealed partial class ArchitecturalLevelAnalyzer
{
	private static Location CreateConfigurationLocation(ConfigurationIssue issue, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		if (issue.LineNumber <= 0)
		{
			return Location.None;
		}

		var file = additionalFiles.FirstOrDefault(candidate => string.Equals(NormalizePath(candidate.Path), NormalizePath(issue.Path), StringComparison.OrdinalIgnoreCase));
		var text = file?.GetText(cancellationToken);
		if (text is null || issue.LineNumber > text.Lines.Count)
		{
			return Location.None;
		}

		var line = text.Lines[issue.LineNumber - 1];
		var character = Math.Max(0, Math.Min(issue.LinePosition - 1, line.Span.Length));
		var position = line.Start + character;
		var result = Location.Create(issue.Path, new TextSpan(position, 0), new LinePositionSpan(new LinePosition(issue.LineNumber - 1, character), new LinePosition(issue.LineNumber - 1, character)));

		return result;
	}

	private static Location CreateConfigurationLocation(string path, int lineNumber, int linePosition, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, CancellationToken cancellationToken)
	{
		if (lineNumber <= 0)
		{
			var inlineAttributeLocation = TryFindInlineSettingsAttributeLocation(compilation);
			var result = inlineAttributeLocation ?? Location.None;

			return result;
		}

		var file = additionalFiles.FirstOrDefault(candidate => string.Equals(NormalizePath(candidate.Path), NormalizePath(path), StringComparison.OrdinalIgnoreCase));
		var text = file?.GetText(cancellationToken);
		if (text is not null && lineNumber <= text.Lines.Count)
		{
			var line = text.Lines[lineNumber - 1];
			var character = Math.Max(0, Math.Min(linePosition - 1, line.Span.Length));
			var position = line.Start + character;
			return Location.Create(path, new TextSpan(position, 0), new LinePositionSpan(new LinePosition(lineNumber - 1, character), new LinePosition(lineNumber - 1, character)));
		}

		var inlineLocation = string.Equals(path, ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey, StringComparison.Ordinal)
			? TryFindInlineSettingsAttributeLocation(compilation)
			: null;
		var fallbackLocation = inlineLocation ?? Location.None;

		return fallbackLocation;
	}

	private static string NormalizePath(string path)
	{
		try
		{
			var result = Path.GetFullPath(path);

			return result;
		}
		catch
		{
			return path;
		}
	}

	private static Location? TryFindInlineSettingsAttributeLocation(Compilation compilation)
	{
		foreach (var attribute in compilation.Assembly.GetAttributes())
		{
			if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), "System.Reflection.AssemblyMetadataAttribute", StringComparison.Ordinal))
			{
				continue;
			}

			if (attribute.ConstructorArguments.Length < 2
			    || !string.Equals(attribute.ConstructorArguments[0].Value as string, ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey, StringComparison.Ordinal))
			{
				continue;
			}

			var syntaxReference = attribute.ApplicationSyntaxReference;
			var location = syntaxReference?.GetSyntax().GetLocation();
			if (location is not null)
			{
				return location;
			}
		}

		return null;
	}
}
