using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationDocumentCollectorTests
{
	[Fact]
	public void Collect_LoadsIncludedDocumentsAndDocumentationItems()
	{
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Root" />
				  <Include path="Shared.anl" />
				</ArchitecturalLevels>
				"""),
			new TestAdditionalText(
				"Shared.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Shared" />
				</ArchitecturalLevels>
				"""));
		var lookup = ArchitectureConfigurationSourceLookup.BuildAdditionalFileLookup(additionalFiles);

		var result = ArchitectureConfigurationDocumentCollector.Collect(
			additionalFiles[0].GetText(TestContext.Current.CancellationToken)!.ToString(),
			additionalFiles[0].Path,
			lookup,
			TestContext.Current.CancellationToken,
			ValidateDocument,
			ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey,
			false);

		result.Documents.Should().HaveCount(2);
		result.Elements.Should().HaveCount(2);
		result.Elements.Select(element => element.Element.Attribute("name")?.Value).Should().BeEquivalentTo(["Root", "Shared"]);
		result.DocumentationItems.Should().Contain(item => item.Kind == "Include" && item.Label == "Shared.anl");
		result.Issues.Should().BeEmpty();
	}

	[Fact]
	public void Collect_LoadsIncludedDocuments_ForWindowsStylePaths()
	{
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText(
				@"D:\repo\config\Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Root" />
				  <Include path="Shared.anl" />
				</ArchitecturalLevels>
				"""),
			new TestAdditionalText(
				@"D:\repo\config\Shared.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Shared" />
				</ArchitecturalLevels>
				"""));
		var lookup = ArchitectureConfigurationSourceLookup.BuildAdditionalFileLookup(additionalFiles);

		var result = ArchitectureConfigurationDocumentCollector.Collect(
			additionalFiles[0].GetText(TestContext.Current.CancellationToken)!.ToString(),
			additionalFiles[0].Path,
			lookup,
			TestContext.Current.CancellationToken,
			ValidateDocument,
			ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey,
			false);

		result.Documents.Should().HaveCount(2);
		result.Elements.Should().HaveCount(2);
		result.Elements.Select(element => element.Element.Attribute("name")?.Value).Should().BeEquivalentTo(["Root", "Shared"]);
		result.Issues.Should().BeEmpty();
	}

	[Fact]
	public void NormalizePath_CollapsesParentSegments_ForWindowsAbsolutePaths()
	{
		var result = ArchitectureConfigurationSourceLookup.NormalizePath(@"D:\repo\Examples\Data\..\Architecture.anl");

		result.Should().Be("D:/repo/Examples/Architecture.anl");
	}

	[Fact]
	public void NormalizePath_CollapsesParentSegments_ForUnixAbsolutePaths()
	{
		var result = ArchitectureConfigurationSourceLookup.NormalizePath("/repo/examples/data/../Architecture.anl");

		result.Should().Be("/repo/examples/Architecture.anl");
	}

	[Fact]
	public void Collect_ReportsMissingIncludedDocument()
	{
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Include path="Missing.anl" />
				</ArchitecturalLevels>
				"""));
		var lookup = ArchitectureConfigurationSourceLookup.BuildAdditionalFileLookup(additionalFiles);

		var result = ArchitectureConfigurationDocumentCollector.Collect(
			additionalFiles[0].GetText(TestContext.Current.CancellationToken)!.ToString(),
			additionalFiles[0].Path,
			lookup,
			TestContext.Current.CancellationToken,
			ValidateDocument,
			ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey,
			false);

		result.Documents.Should().HaveCount(1);
		result.Issues.Should().ContainSingle(issue => issue.Message.Contains("Included architecture configuration was not provided as an AdditionalFile", StringComparison.Ordinal));
	}

	[Fact]
	public void Collect_LoadsIncludedDocuments_ForRelativeBackslashPaths()
	{
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText(
				@"Examples\Scenarios\Example.ProjectReferenceBoundaries\Example.ProjectReferenceBoundaries.Application\Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Include path="../Architecture.anl" />
				</ArchitecturalLevels>
				"""),
			new TestAdditionalText(
				@"Examples\Scenarios\Example.ProjectReferenceBoundaries\Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Application" />
				</ArchitecturalLevels>
				"""));
		var lookup = ArchitectureConfigurationSourceLookup.BuildAdditionalFileLookup(additionalFiles);

		var result = ArchitectureConfigurationDocumentCollector.Collect(
			additionalFiles[0].GetText(TestContext.Current.CancellationToken)!.ToString(),
			additionalFiles[0].Path,
			lookup,
			TestContext.Current.CancellationToken,
			ValidateDocument,
			ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey,
			false);

		result.Documents.Should().HaveCount(2);
		result.Elements.Should().ContainSingle();
		result.Elements[0].Element.Attribute("name")?.Value.Should().Be("Application");
		result.Issues.Should().BeEmpty();
	}

	private static ImmutableArray<ConfigurationIssue> ValidateDocument(XDocument document, string configPath)
	{
		_ = document;
		_ = configPath;

		return ImmutableArray<ConfigurationIssue>.Empty;
	}

	private sealed class TestAdditionalText(string path, string content) : AdditionalText
	{
		private readonly SourceText text = SourceText.From(content);

		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = text;

			return result;
		}
	}
}
