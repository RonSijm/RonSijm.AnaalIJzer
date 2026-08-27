using RonSijm.AnaalIJzer.Core.Editor.QuickInfo;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.EditorRuntime.Tests.Editor.Snapshots;

public sealed partial class EditorSnapshotTests
{
	[Fact]
	public async Task Snapshot_ExposesApiSurfaceLeakForQuickInfo()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface description="Public contracts only.">
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
				public LollyQueryable OrderRaw() => null!;
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.ApiSurfaceIndicators.Should().ContainSingle().Which;

		indicator.ApiMemberName.Should().Be("CandyService.OrderRaw");
		indicator.CallerLayerPath.Should().Be("Application");
		indicator.ExposedTypeName.Should().Be("LollyQueryable");
		indicator.ExposedLayerPath.Should().Be("QuerySurface");
		indicator.Site.Should().Be("MethodReturn");
		indicator.DiagnosticId.Should().Be(ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
		var content = ArchitectureQuickInfoContentBuilder.CreateApiSurfaceContent(indicator).ToString();
		content.Should().Contain("Diagnostic: ARCH009");
		content.Should().Contain("Public contracts only");
		content.Should().Contain("Exposed type: LollyQueryable (QuerySurface)");
	}

	[Fact]
	public async Task Snapshot_DoesNotLabelPrivateApiSurfaceUse()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface><BlockedLayer path="/QuerySurface" /></ApiSurface>
			  </Layer>
			  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
				private LollyQueryable BuildQuery() => null!;
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.ApiSurfaceIndicators.Should().BeEmpty();
	}

	[Fact]
	public async Task Snapshot_ExposesTransitiveApiSurfacePathForQuickInfo()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface description="Contracts may not reveal query surfaces.">
			      <TransitiveExposure maxDepth="3" description="Inspect public contract members." />
			      <AllowedLayer path="/Contracts" />
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts"><Class endsWith="Receipt" /></Layer>
			  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt OrderRaw() => new();
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.ApiSurfaceIndicators.Should().ContainSingle(item => item.DiagnosticId == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Which;

		indicator.ExposureDepth.Should().Be(1);
		indicator.ExposurePath.Should().Contain("CandyService.OrderRaw");
		indicator.ExposurePath.Should().Contain("CandyReceipt.RawQuery");
		indicator.ExposureSegments.Should().ContainSingle();
		var content = ArchitectureQuickInfoContentBuilder.CreateApiSurfaceContent(indicator).ToString();
		content.Should().Contain("AnaalIJzer transitive API exposure");
		content.Should().Contain("Exposure depth: 1");
		content.Should().Contain("Inspect public contract members.");
		content.Should().Contain("Source-backed path segments: 1");
	}

	[Fact]
	public async Task Snapshot_ProjectEvidence_ContainsTransitiveApiSurfaceViolation()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <TransitiveExposure maxDepth="3" description="Inspect public contract members." />
			      <AllowedLayer path="/Contracts" />
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts"><Class endsWith="Receipt" /></Layer>
			  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt OrderRaw() => new();
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config, includeProjectEvidence: true);
		var evidence = snapshot.GraphSnapshot.Evidence.Dependencies.Should().ContainSingle(item =>
			item.DiagnosticId == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Which;

		evidence.CallerLayerPath.Should().Be("Application");
		evidence.DependencyLayerPath.Should().Be("QuerySurface");
		evidence.Site.Should().Be("Property");
		evidence.ExposureDepth.Should().Be(1);
		evidence.ExposurePath.Should().Contain("CandyService.OrderRaw");
		evidence.ExposurePath.Should().Contain("CandyReceipt.RawQuery");
	}
}
