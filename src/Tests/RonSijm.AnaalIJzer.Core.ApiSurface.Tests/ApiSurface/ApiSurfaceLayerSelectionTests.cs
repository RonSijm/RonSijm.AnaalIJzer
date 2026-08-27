using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Tests.ApiSurface;

public sealed class ApiSurfaceLayerSelectionTests
{
	[Fact]
	public void UnrecognizedSelection_HasNoLayerAndMatchesNothing()
	{
		var result = ApiSurfaceLayerSelection.Unrecognized;

		result.IsRecognized.Should().BeFalse();
		result.LayerPath.Should().BeEmpty();
		result.LayerPaths.Should().BeEmpty();
		result.Selects("Contracts").Should().BeFalse();
	}

	[Fact]
	public void RecognizedSelection_SelectsAnyAncestorPath()
	{
		var result = new ApiSurfaceLayerSelection(
			"Application/Contracts",
            ["Application", "Application/Contracts"]);

		result.IsRecognized.Should().BeTrue();
		result.Selects("Application").Should().BeTrue();
		result.Selects("Application/Contracts").Should().BeTrue();
		result.Selects("Repository").Should().BeFalse();
	}
}
