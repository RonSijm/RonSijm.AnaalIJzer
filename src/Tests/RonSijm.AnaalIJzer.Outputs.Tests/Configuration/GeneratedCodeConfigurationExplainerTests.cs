using RonSijm.AnaalIJzer.Outputs.Configuration;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Configuration;

public sealed class GeneratedCodeConfigurationExplainerTests
{
	[Fact]
	public void Explainer_ExplainsGeneratedCodeScopeAndPathMatchers()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <GeneratedCode mode="IncludeConfigured" maximumDocumentLength="8192" description="Only owned generated output is checked.">
				    <Path endsWith="Generated_Clock_Kitchen.g.cs" />
				  </GeneratedCode>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Generated-code analysis uses `IncludeConfigured` mode with a maximum document length of `8192` characters.");
			markdown.Should().Contain("Includes generated paths matching endsWith=\"Generated_Clock_Kitchen.g.cs\".");
			markdown.Should().Contain("Only owned generated output is checked.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
