using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Config.Compilation;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ArchitectureConfigurationDocumentParseContext CollectDocumentContext(
		string content,
		string configPath,
		ImmutableArray<AdditionalText> additionalFiles,
		CancellationToken cancellationToken,
		ImmutableArray<ConfigurationIssue>.Builder issues,
		bool isInlineConfiguration)
	{
		var additionalFileLookup = ArchitectureConfigurationSourceLookup.BuildAdditionalFileLookup(additionalFiles);
		var collected = ArchitectureConfigurationDocumentCollector.Collect(
			content,
			configPath,
			additionalFileLookup,
			cancellationToken,
			ArchitectureConfigurationValidator.Validate,
			InlineSettingsMetadataKey,
			isInlineConfiguration);
		issues.AddRange(collected.Issues);

		var result = new ArchitectureConfigurationDocumentParseContext(
			collected.Documents.Select(document => new ArchitectureConfigurationDocumentInput(document.Root, document.Path, document.IsInlineConfiguration)).ToImmutableArray(),
			collected.Elements.Select(element => new ArchitectureConfigurationElementInput(element.Element, element.Path, element.IsInlineConfiguration)).ToImmutableArray(),
			collected.DocumentationItems);

		return result;
	}

	private static ArchitectureConfigurationRootSettings ParseRootSettings(
		ImmutableArray<ArchitectureConfigurationDocumentInput> documents,
		string configPath,
		ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var requiredRecognizedDependencySites = ParseRequiredRecognizedDependencySites(documents, issues);
		var exceptionPolicy = ParseExceptionPolicy(documents, issues);
		var enforceAcyclic = ParseRootBooleanFlag(documents, "enforceAcyclic", issues);
		var enforceObservedAcyclic = ParseRootBooleanFlag(documents, "enforceObservedAcyclic", issues);

		var enableReport = TryFindEnabledDocument(documents, "enableReport", out var reportRoot, out var reportConfigPath);
		var reportPath = ArchitectureConfigurationSourceLookup.ResolveRelativePath(
			reportRoot?.Attribute("reportPath")?.Value ?? "architectural-violations.md",
			reportConfigPath ?? configPath);

		var enableDocumentation = TryFindEnabledDocument(documents, "enableDocumentation", out var documentationRoot, out var documentationConfigPath);
		var documentationPath = ArchitectureConfigurationSourceLookup.ResolveRelativePath(
			documentationRoot?.Attribute("documentationPath")?.Value ?? "architecture-documentation.md",
			documentationConfigPath ?? configPath);

		var result = new ArchitectureConfigurationRootSettings(
			requiredRecognizedDependencySites,
			exceptionPolicy,
			enforceAcyclic,
			enforceObservedAcyclic,
			new OutputConfig(enableReport, reportPath, enableDocumentation, documentationPath));

		return result;
	}
}

