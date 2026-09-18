using RonSijm.AnaalIJzer.Outputs.Configuration;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Configuration;

public sealed class NamespaceHierarchyPolicyExplainerTests
{
	[Fact]
	public void Explainer_ExplainsNamespaceHierarchyRelationshipsAndSiteFilters()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <NamespaceHierarchyPolicy rootNamespace="Restaurant" description="Feature namespaces own their recipes.">
				    <BlockedRelation relation="DescendantToAncestor" allowedSites="Constructor, Field" description="Orders do not reach back into the restaurant root." />
				    <BlockedRelation relation="SiblingToSibling" blockedSites="Method" />
				  </NamespaceHierarchyPolicy>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Namespace hierarchy policy protects namespace ownership below `Restaurant`");
			markdown.Should().Contain("Blocks `DescendantToAncestor` dependencies (allowedSites=\"Constructor, Field\")");
			markdown.Should().Contain("Blocks `SiblingToSibling` dependencies (blockedSites=\"Method\")");
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
