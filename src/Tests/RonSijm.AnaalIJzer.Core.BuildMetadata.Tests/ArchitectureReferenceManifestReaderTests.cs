using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.Core.BuildMetadata.Tests;

public sealed class ArchitectureReferenceManifestReaderTests
{
	[Fact]
	public void Read_ParsesProjectAndPackageRecords()
	{
		var manifestText = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			@"Project	D:\src\Shop.Web.csproj	D:\src\Shop.Application.csproj",
			@"Package	D:\src\Shop.Application.csproj	Microsoft.Extensions.Logging	9.0.0	Direct");
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

		var manifest = ArchitectureReferenceManifestReader.Read(manifestText, "ArchitectureReferenceManifest.txt", issues);

		issues.Should().BeEmpty();
		manifest.ProjectReferences.Should().ContainSingle();
		manifest.PackageReferences.Should().ContainSingle();
		manifest.ProjectReferences[0].SourceProjectPath.Should().Be(@"D:\src\Shop.Web.csproj");
		manifest.ProjectReferences[0].TargetProjectPath.Should().Be(@"D:\src\Shop.Application.csproj");
		manifest.PackageReferences[0].SourceProjectPath.Should().Be(@"D:\src\Shop.Application.csproj");
		manifest.PackageReferences[0].PackageId.Should().Be("Microsoft.Extensions.Logging");
		manifest.PackageReferences[0].PackageVersion.Should().Be("9.0.0");
		manifest.PackageReferences[0].ReferenceKind.Should().Be(PackageReferenceKind.Direct);
	}

	[Fact]
	public void Read_WhenHeaderIsUnsupported_ReportsSingleIssue_AndReturnsEmptyManifest()
	{
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

		var manifest = ArchitectureReferenceManifestReader.Read("NotTheRightHeader", "ArchitectureReferenceManifest.txt", issues);

		manifest.ProjectReferences.Should().BeEmpty();
		manifest.PackageReferences.Should().BeEmpty();
		issues.Should().ContainSingle();
		issues[0].Kind.Should().Be(ConfigurationIssueKind.InvalidConfiguration);
		issues[0].Message.Should().Contain("unsupported header");
	}

	[Fact]
	public void Read_DeduplicatesRepeatedMalformedRecords()
	{
		var manifestText = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			@"Package	D:\src\Shop.Domain.csproj	Microsoft.Extensions.Logging",
			@"Package	D:\src\Shop.Domain.csproj	Microsoft.Extensions.Logging");
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

		_ = ArchitectureReferenceManifestReader.Read(manifestText, "ArchitectureReferenceManifest.txt", issues);

		issues.Should().ContainSingle();
		issues[0].Message.Should().Be("Package reference manifest entries must contain exactly five tab-delimited columns.");
	}

	[Fact]
	public void Read_IgnoresLegacyProjectRowsWithEmptyTarget()
	{
		var manifestText = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			"Project\tD:\\src\\Shop.Web.csproj\t");
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

		var manifest = ArchitectureReferenceManifestReader.Read(manifestText, "ArchitectureReferenceManifest.txt", issues);

		issues.Should().BeEmpty();
		manifest.ProjectReferences.Should().BeEmpty();
		manifest.PackageReferences.Should().BeEmpty();
	}
}
