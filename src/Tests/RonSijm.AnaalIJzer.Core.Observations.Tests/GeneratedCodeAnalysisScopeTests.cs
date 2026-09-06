using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.Core.Observations.Tests;

public sealed class GeneratedCodeAnalysisScopeTests
{
	[Fact]
	public void Exclude_DoesNotAnalyzeGeneratedSource_ButKeepsOrdinarySourceInScope()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var generatedTree = CreateSyntaxTree("Generated_Clock_Kitchen.g.cs", "public class GeneratedClockKitchen { }");
		var ordinaryTree = CreateSyntaxTree("PizzaKitchen.cs", "public class PizzaKitchen { }");

		GeneratedCodeAnalysisScope.Exclude.ShouldAnalyze(generatedTree, cancellationToken).Should().BeFalse();
		GeneratedCodeAnalysisScope.Exclude.ShouldAnalyze(ordinaryTree, cancellationToken).Should().BeTrue();
	}

	[Fact]
	public void IncludeAll_AnalyzesGeneratedSourceBelowConfiguredSizeLimit()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var scope = new GeneratedCodeAnalysisScope(GeneratedCodeAnalysisMode.IncludeAll, ImmutableArray<GeneratedCodePathRule>.Empty, 40);
		var smallTree = CreateSyntaxTree("Generated_Clock_Kitchen.g.cs", "public class Kitchen { }");
		var oversizedTree = CreateSyntaxTree("Generated_Clock_Kitchen.g.cs", new string('x', 41));

		scope.ShouldAnalyze(smallTree, cancellationToken).Should().BeTrue();
		scope.ShouldAnalyze(oversizedTree, cancellationToken).Should().BeFalse();
	}

	[Fact]
	public void IncludeConfigured_AnalyzesOnlyMatchingGeneratedSource()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var paths = ImmutableArray.Create(new GeneratedCodePathRule(
			ImmutableArray.Create(new MatchCondition(MatchKind.EndsWith, "Generated_Clock_Kitchen.g.cs"))));
		var scope = new GeneratedCodeAnalysisScope(GeneratedCodeAnalysisMode.IncludeConfigured, paths);
		var matchingTree = CreateSyntaxTree("Generated_Clock_Kitchen.g.cs", "public class Kitchen { }");
		var nonMatchingTree = CreateSyntaxTree("Generated_Menu_Kitchen.g.cs", "public class Kitchen { }");

		scope.ShouldAnalyze(matchingTree, cancellationToken).Should().BeTrue();
		scope.ShouldAnalyze(nonMatchingTree, cancellationToken).Should().BeFalse();
	}

	[Fact]
	public void CompilationTypeCollector_UsesGeneratedCodeScope()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var compilation = CSharpCompilation.Create(
			"GeneratedCodeScopeTests",
			[
				CreateSyntaxTree("PizzaKitchen.cs", "public class PizzaKitchen { }"),
				CreateSyntaxTree("Generated_Clock_Kitchen.g.cs", "public class GeneratedClockKitchen { }")
			]);
		var includeAll = new GeneratedCodeAnalysisScope(GeneratedCodeAnalysisMode.IncludeAll, ImmutableArray<GeneratedCodePathRule>.Empty);

		var excludedTypes = CompilationTypeCollector.GetProjectTypes(compilation, GeneratedCodeAnalysisScope.Exclude, cancellationToken);
		var includedTypes = CompilationTypeCollector.GetProjectTypes(compilation, includeAll, cancellationToken);

		excludedTypes.Select(type => type.Name).Should().Equal("PizzaKitchen");
		includedTypes.Select(type => type.Name).Should().Equal("GeneratedClockKitchen", "PizzaKitchen");
	}

	private static Microsoft.CodeAnalysis.SyntaxTree CreateSyntaxTree(string fileName, string source)
	{
		var result = CSharpSyntaxTree.ParseText(SourceText.From(source), path: fileName);

		return result;
	}
}
