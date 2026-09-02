using AwesomeAssertions;
using RonSijm.AnaalIJzer.Application;
using RonSijm.AnaalIJzer.IntegrationTests.Support;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests;

public sealed class ExampleConfigurationFixIntegrationTests
{
	[Fact]
	public async Task ExampleProjects_ExposeExpectedConfigurationFixProposals()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var context = ExampleRepositoryContext.Discover();
		var failures = new List<string>();

		using var generatedFiles = new GeneratedExampleFilesScope(context);
		var runner = new ApplicationRunner();

		foreach (var expectation in ExampleFixExpectationCatalog.All)
		{
			var projectPath = context.GetExampleProjectPath(expectation.RelativeProjectPath);
			var result = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			if (expectation.ExpectNoProposals)
			{
				if (result.FixProposals.Length > 0)
				{
					failures.Add($"{expectation.RelativeProjectPath}: expected no config fix proposals, but got {string.Join(", ", result.FixProposals.Select(proposal => proposal.Title))}.");
				}

				if (!string.IsNullOrWhiteSpace(result.Content)
				    && !result.Content.Contains("No configuration-backed fix proposals are currently available", StringComparison.Ordinal))
				{
					failures.Add($"{expectation.RelativeProjectPath}: expected the empty-proposal report text, got:{Environment.NewLine}{result.Content}");
				}

				continue;
			}

			foreach (var expectedTitle in expectation.ExpectedTitles)
			{
				var proposal = result.FixProposals.FirstOrDefault(item => string.Equals(item.Title, expectedTitle, StringComparison.Ordinal));
				if (proposal is null)
				{
					failures.Add($"{expectation.RelativeProjectPath}: missing expected fix proposal '{expectedTitle}'. Available proposals:{Environment.NewLine}{string.Join(Environment.NewLine, result.FixProposals.Select(item => "- " + item.Title))}");
					continue;
				}

				if (string.IsNullOrWhiteSpace(proposal.PreviewDiff))
				{
					failures.Add($"{expectation.RelativeProjectPath}: proposal '{expectedTitle}' should provide a preview diff.");
				}

				if (!string.IsNullOrWhiteSpace(expectation.ExpectedTargetSuffix)
				    && !HasExpectedTargetSuffix(proposal.TargetPath, expectation.ExpectedTargetSuffix))
				{
					failures.Add($"{expectation.RelativeProjectPath}: proposal '{expectedTitle}' should target '*{expectation.ExpectedTargetSuffix}', got '{proposal.TargetPath}'.");
				}
			}
		}

		failures.Should().BeEmpty("every example fixer scenario should expose the intended configuration fix proposals:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine + Environment.NewLine, failures));
	}

	[Theory]
	[InlineData(@"D:\source\RonSijm\RonSijm.AnaalIJzer\Examples\Diagnostics\Example.Arch004.WrongDirection\Example.cs", @"Diagnostics\Example.Arch004.WrongDirection\Example.cs")]
	[InlineData("/home/runner/work/RonSijm.AnaalIJzer/RonSijm.AnaalIJzer/Examples/Diagnostics/Example.Arch004.WrongDirection/Example.cs", @"Diagnostics\Example.Arch004.WrongDirection\Example.cs")]
	public void ProposalTargetPath_MatchesPlatformIndependentSuffix(string targetPath, string expectedTargetSuffix)
	{
		var result = HasExpectedTargetSuffix(targetPath, expectedTargetSuffix);

		result.Should().BeTrue();
	}

	private static bool HasExpectedTargetSuffix(string targetPath, string expectedTargetSuffix)
	{
		var normalizedTargetPath = targetPath.Replace('\\', '/');
		var normalizedExpectedSuffix = expectedTargetSuffix.Replace('\\', '/');
		var result = normalizedTargetPath.EndsWith(normalizedExpectedSuffix, StringComparison.OrdinalIgnoreCase);

		return result;
	}
}
