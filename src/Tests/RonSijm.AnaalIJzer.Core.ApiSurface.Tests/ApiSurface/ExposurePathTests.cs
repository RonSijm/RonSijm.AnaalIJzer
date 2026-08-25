using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Tests.ApiSurface;

public sealed class ExposurePathTests
{
	[Fact]
	public void ToDisplayText_FormatsTheFullTraversalChain()
	{
		var path = new ExposurePath("CandyService.OrderRaw", ImmutableArray<ExposurePathSegment>.Empty)
			.Append(new ExposurePathSegment("CandyReceipt.RawQuery", Location.None))
			.Append(new ExposurePathSegment("LollyEnvelope.CurrentQuery", null));

		var result = path.ToDisplayText("LollyQueryable");

		result.Should().Be("CandyService.OrderRaw -> CandyReceipt.RawQuery -> LollyEnvelope.CurrentQuery -> LollyQueryable");
	}

	[Fact]
	public void Segment_PreservesDisplayNameAndLocation()
	{
		var segment = new ExposurePathSegment("CandyReceipt.RawQuery", Location.None);

		segment.DisplayName.Should().Be("CandyReceipt.RawQuery");
		segment.Location.Should().Be(Location.None);
	}
}
