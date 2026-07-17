using RonSijm.AnaalIJzer.Arse.Components;

namespace RonSijm.AnaalIJzer.Arse.Tests.Components;

public sealed class PathSuggestionProviderTests
{
	[Fact]
	public void Find_FiltersFilesButKeepsDirectoriesAvailableForTraversal()
	{
		var directory = CreateTestDirectory();
		try
		{
			Directory.CreateDirectory(Path.Combine(directory, "Architecture"));
			File.WriteAllText(Path.Combine(directory, "Architecture.anl"), string.Empty);
			File.WriteAllText(Path.Combine(directory, "Architecture.csproj"), string.Empty);

			var result = PathSuggestionProvider.Find("Arch", PathSelectionMode.File, [".anl"], workingDirectory: directory);

			result.Suggestions.Select(suggestion => suggestion.DisplayValue).Should().Equal(
				"Architecture" + Path.DirectorySeparatorChar,
				"Architecture.anl");
			result.CompletionValue.Should().Be("Architecture");
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Fact]
	public void Find_InDirectoryModeExcludesFilesAndCompletesTheDirectorySeparator()
	{
		var directory = CreateTestDirectory();
		try
		{
			Directory.CreateDirectory(Path.Combine(directory, "Architecture"));
			File.WriteAllText(Path.Combine(directory, "Architecture.anl"), string.Empty);

			var result = PathSuggestionProvider.Find("Arch", PathSelectionMode.Directory, workingDirectory: directory);

			result.Suggestions.Should().ContainSingle();
			result.Suggestions[0].IsDirectory.Should().BeTrue();
			result.CompletionValue.Should().Be("Architecture" + Path.DirectorySeparatorChar);
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Fact]
	public void Find_CompletesOnlyTheCurrentMultipleInputSegment()
	{
		var directory = CreateTestDirectory();
		try
		{
			File.WriteAllText(Path.Combine(directory, "Architecture.anl"), string.Empty);

			var result = PathSuggestionProvider.Find("First.anl; Arch", PathSelectionMode.File, [".anl"], allowMultiple: true, workingDirectory: directory);

			result.Suggestions.Should().ContainSingle();
			result.Suggestions[0].Value.Should().Be("First.anl; Architecture.anl");
			result.Suggestions[0].DisplayValue.Should().Be("Architecture.anl");
			result.CompletionValue.Should().Be("First.anl; Architecture.anl");
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	private static string CreateTestDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-path-suggestions-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}
