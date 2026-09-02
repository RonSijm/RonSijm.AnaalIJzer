using AwesomeAssertions;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed class MatcherAttributeOptionsTests
{
	[Fact]
	public void GetNames_UsesTheSharedTypeMatcherProfileForClasses()
	{
		var result = MatcherAttributeOptions.GetNames("Class");

		result.Should().Contain("withAttribute");
		result.Should().Contain("typeKind");
		result.Should().NotContain("value");
	}

	[Fact]
	public void GetNames_UsesTheSharedNamespaceMatcherProfileForNamespacesAndAssemblies()
	{
		var namespaceResult = MatcherAttributeOptions.GetNames("Namespace");
		var assemblyResult = MatcherAttributeOptions.GetNames("Assembly");

		namespaceResult.Should().BeEquivalentTo(assemblyResult, options => options.WithStrictOrdering());
		namespaceResult.Should().Contain("exactName");
		namespaceResult.Should().NotContain("withAttribute");
		namespaceResult.Should().NotContain("typeKind");
	}
}
