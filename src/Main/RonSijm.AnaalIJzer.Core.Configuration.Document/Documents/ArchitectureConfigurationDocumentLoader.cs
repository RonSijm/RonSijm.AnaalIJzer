using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static class ArchitectureConfigurationDocumentLoader
{
	public const string ConfigFileName = "Architecture.anl";
	public const string InlineSettingsMetadataKey = "AnaalIJzerSettings";

	public static AdditionalText? FindConfigurationFile(ImmutableArray<AdditionalText> additionalFiles)
	{
		var result = additionalFiles.FirstOrDefault(file => ArchitectureConfigurationSourceLookup.GetFileNamePreservingStyle(file.Path).Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase));

		return result;
	}

	public static ArchitectureConfigurationTextDocument? TryReadAnalyzerConfigurationText(ImmutableArray<AdditionalText> additionalFiles, Compilation? compilation, string? inlineConfigPath, CancellationToken cancellationToken)
	{
		var configFile = FindConfigurationFile(additionalFiles);
		if (configFile is not null)
		{
			var content = configFile.GetText(cancellationToken)?.ToString();
			if (string.IsNullOrWhiteSpace(content))
			{
				return null;
			}

			return new ArchitectureConfigurationTextDocument(content!, configFile.Path);
		}

		var result = ArchitectureConfigurationSourceDiscovery.TryReadInlineConfigurationTextDocument(compilation, inlineConfigPath, cancellationToken);

		return result;
	}

	public static string? TryReadInlineConfigurationXml(Compilation? compilation)
	{
		if (compilation is null)
		{
			return null;
		}

		foreach (var attribute in compilation.Assembly.GetAttributes())
		{
			if (!IsAssemblyMetadataAttribute(attribute.AttributeClass))
			{
				continue;
			}

			if (attribute.ConstructorArguments.Length >= 2
			    && string.Equals(attribute.ConstructorArguments[0].Value as string, InlineSettingsMetadataKey, StringComparison.Ordinal)
			    && attribute.ConstructorArguments[1].Value is string xml)
			{
				return xml;
			}
		}

		return null;
	}

	public static bool IsAssemblyMetadataAttribute(INamedTypeSymbol? attributeClass)
	{
		var result = attributeClass is not null
		             && string.Equals(attributeClass.Name, "AssemblyMetadataAttribute", StringComparison.Ordinal)
		             && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), "System.Reflection", StringComparison.Ordinal);

		return result;
	}

	public static string FindInlineConfigurationSourcePath(Compilation compilation, string? fallbackPath, CancellationToken cancellationToken)
	{
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var root = syntaxTree.GetRoot(cancellationToken);
			if (!ContainsInlineSettingsMetadata(root))
			{
				continue;
			}

			if (!string.IsNullOrWhiteSpace(syntaxTree.FilePath))
			{
				return syntaxTree.FilePath;
			}

			return fallbackPath ?? string.Empty;
		}

		return string.Empty;
	}

	public static bool ContainsInlineSettingsMetadata(SyntaxNode root)
	{
		foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
		{
			if (!InlineAssemblyMetadataSettings.IsAssemblyMetadataAttribute(attribute))
			{
				continue;
			}

			var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
			if (firstArgument is not null && InlineAssemblyMetadataSettings.IsAnaalIJzerSettingsKey(firstArgument))
			{
				return true;
			}
		}

		return false;
	}

	public static bool TryReadConfigurationDocument(ArchitectureConfigurationSource source, out XDocument? document, out string message)
	{
		if (!source.CanEdit)
		{
			document = null;
			message = "This configuration source cannot be inspected.";
			return false;
		}

		if (source.Kind == ArchitectureConfigurationSourceKind.XmlFile)
		{
			if (!File.Exists(source.Path))
			{
				document = null;
				message = "Architecture configuration file does not exist: " + source.Path;
				return false;
			}

			document = LoadXmlFile(source.Path);
			message = "Loaded architecture configuration.";
			return true;
		}

		if (source.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata)
		{
			if (!File.Exists(source.Path))
			{
				document = null;
				message = "Inline settings source file does not exist: " + source.Path;
				return false;
			}

			var sourceText = File.ReadAllText(source.Path);
			if (!InlineAssemblyMetadataSettings.TryFindInlineSettings(sourceText, out var settings, out message))
			{
				document = null;
				return false;
			}

			document = XDocument.Parse(settings.Xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
			message = "Loaded inline architecture configuration.";
			return true;
		}

		document = null;
		message = "This configuration source cannot be inspected.";
		return false;
	}

	public static XDocument LoadXmlFile(string path)
	{
		var xml = ReadXmlText(path);
		var result = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

		return result;
	}

	public static string ReadXmlText(string path)
	{
		using var reader = new StreamReader(path, Encoding.UTF8, true);
		var result = reader.ReadToEnd();

		return result;
	}
}
