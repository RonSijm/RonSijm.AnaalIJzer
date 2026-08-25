using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Config.Compilation;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.ProjectArchitecture;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RoslynCompilation = Microsoft.CodeAnalysis.Compilation;

namespace RonSijm.AnaalIJzer.Config.Parsing;

/// <summary>
///     Parses an <c>Architecture.anl</c> additional file or inline
///     <c>AssemblyMetadata("AnaalIJzerSettings", ...)</c> value into an <see cref="AnalyzerConfig" />.
	/// </summary>
public static partial class ArchitecturalConfigParser
{
	public const string ConfigFileName = ArchitectureConfigurationDocumentLoader.ConfigFileName;
	public const string InlineSettingsMetadataKey = ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey;

	public static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		var result = Parse(additionalFiles, null, cancellationToken);

		return result;
	}

	public static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, RoslynCompilation? compilation, CancellationToken cancellationToken)
	{
		var result = Parse(additionalFiles, compilation, null, cancellationToken);

		return result;
	}

	public static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, RoslynCompilation? compilation, string? inlineConfigPath, CancellationToken cancellationToken)
	{
		var document = ArchitectureConfigurationDocumentLoader.TryReadAnalyzerConfigurationText(additionalFiles, compilation, inlineConfigPath, cancellationToken);
		if (document is null || string.IsNullOrWhiteSpace(document.Content))
		{
			return AnalyzerConfig.Empty;
		}

		return ParseXml(document.Content, document.Path, additionalFiles, cancellationToken, document.IsInlineConfiguration);
	}

	public static AnalyzerConfig ParseFile(AdditionalText configFile, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		var content = configFile.GetText(cancellationToken)?.ToString();
		return string.IsNullOrWhiteSpace(content) ? AnalyzerConfig.Empty : ParseXml(content!, configFile.Path, additionalFiles, cancellationToken, false);
	}

	public static AdditionalText? FindConfigFile(ImmutableArray<AdditionalText> additionalFiles)
	{
		var result = ArchitectureConfigurationDocumentLoader.FindConfigurationFile(additionalFiles);

		return result;
	}

	private static AnalyzerConfig ParseXml(string content, string configPath, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken, bool isInlineConfiguration)
	{
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();
		try
		{
			var documentContext = CollectDocumentContext(content, configPath, additionalFiles, cancellationToken, issues, isInlineConfiguration);
			if (!TryContinueAfterDocumentIntake(documentContext, issues, out var earlyResult))
			{
				return earlyResult;
			}

			var rootSettings = ParseRootSettings(documentContext.Documents, configPath, issues);
			var materialization = ArchitectureConfigurationMaterializer.Materialize(documentContext.Elements, rootSettings, configPath, issues);
			var result = ArchitectureAnalyzerConfigFactory.Create(documentContext, rootSettings, materialization, issues);

			return result;
		}
		catch (XmlException ex)
		{
			return AnalyzerConfig.Invalid(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Invalid architecture XML: {ex.Message}", configPath, ex.LineNumber, ex.LinePosition));
		}
		catch (Exception ex)
		{
			return AnalyzerConfig.Invalid(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Could not read architecture configuration: {ex.Message}", configPath, 0, 0));
		}
	}

}
