using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Diagnostics.CodeFixes;
using RonSijm.AnaalIJzer.Workspace.Analysis.ConfigurationFixes;

namespace RonSijm.AnaalIJzer.Workspace.Analysis;

internal sealed partial class ProjectAnalysisHost
{
	public async Task<ConfigurationFixCollectionResult> FindProjectConfigurationFixesAsync(string projectPath, CancellationToken cancellationToken)
	{
		EnsureRestored(projectPath);
		_workspaceFailures.Clear();
		var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
		var analysis = await AnalyzeProjectAsync(project, projectPath, cancellationToken);
		var resolved = await CollectResolvedConfigurationFixesAsync(project, analysis, cancellationToken);
		var result = CreateCollectionResult(analysis.ProjectDirectory, analysis.AnalyzerDiagnostics.Length, resolved);

		return result;
	}

	public async Task<ConfigurationFixCollectionResult> FindSolutionConfigurationFixesAsync(string solutionPath, CancellationToken cancellationToken)
	{
		EnsureSolutionRestored(solutionPath);
		_workspaceFailures.Clear();
		var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
		var solutionConfigFile = FindSolutionConfigFile(solutionPath, cancellationToken);
		var allResolved = ImmutableArray.CreateBuilder<ResolvedConfigurationFix>();
		var diagnosticCount = 0;
		foreach (var project in solution.Projects
			         .Where(candidate => candidate.Language == LanguageNames.CSharp)
			         .OrderBy(candidate => candidate.FilePath ?? candidate.Name, StringComparer.OrdinalIgnoreCase))
		{
			var analysis = await AnalyzeProjectAsync(project, project.FilePath ?? solutionPath, cancellationToken, solutionConfigFile);
			diagnosticCount += analysis.AnalyzerDiagnostics.Length;
			allResolved.AddRange(await CollectResolvedConfigurationFixesAsync(project, analysis, cancellationToken));
		}

		var result = CreateCollectionResult(Path.GetDirectoryName(solutionPath)!, diagnosticCount, allResolved.ToImmutable());

		return result;
	}

	public async Task<ConfigurationFixApplyResult> ApplyProjectConfigurationFixAsync(string projectPath, string fixId, CancellationToken cancellationToken)
	{
		EnsureRestored(projectPath);
		_workspaceFailures.Clear();
		var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
		var analysis = await AnalyzeProjectAsync(project, projectPath, cancellationToken);
		var resolved = await CollectResolvedConfigurationFixesAsync(project, analysis, cancellationToken);
		var target = resolved.FirstOrDefault(proposal => string.Equals(proposal.Proposal.Id, fixId, StringComparison.Ordinal))
		             ?? throw new InvalidOperationException("Configuration fix not found: " + fixId);

		await ConfigurationFixProposalCollector.PersistChangesAsync(target.OriginalSolution, target.ChangedSolution, analysis.InlineConfigSourcePath, cancellationToken);
		var result = new ConfigurationFixApplyResult(target.Proposal, target.Proposal.ChangedPaths);

		return result;
	}

	public async Task<ConfigurationFixApplyResult> ApplySolutionConfigurationFixAsync(string solutionPath, string fixId, CancellationToken cancellationToken)
	{
		EnsureSolutionRestored(solutionPath);
		_workspaceFailures.Clear();
		var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
		var solutionConfigFile = FindSolutionConfigFile(solutionPath, cancellationToken);
		foreach (var project in solution.Projects
			         .Where(candidate => candidate.Language == LanguageNames.CSharp)
			         .OrderBy(candidate => candidate.FilePath ?? candidate.Name, StringComparer.OrdinalIgnoreCase))
		{
			var analysis = await AnalyzeProjectAsync(project, project.FilePath ?? solutionPath, cancellationToken, solutionConfigFile);
			var resolved = await CollectResolvedConfigurationFixesAsync(project, analysis, cancellationToken);
			var target = resolved.FirstOrDefault(proposal => string.Equals(proposal.Proposal.Id, fixId, StringComparison.Ordinal));
			if (target is null)
			{
				continue;
			}

			await ConfigurationFixProposalCollector.PersistChangesAsync(target.OriginalSolution, target.ChangedSolution, analysis.InlineConfigSourcePath, cancellationToken);
			var result = new ConfigurationFixApplyResult(target.Proposal, target.Proposal.ChangedPaths);

			return result;
		}

		throw new InvalidOperationException("Configuration fix not found: " + fixId);
	}

	private static ConfigurationFixCollectionResult CreateCollectionResult(string workingDirectory, int diagnosticCount, ImmutableArray<ResolvedConfigurationFix> resolved)
	{
		var proposals = resolved.Select(item => item.Proposal).ToImmutableArray();
		var fixableDiagnosticCount = resolved.Select(item => item.DiagnosticKey).Distinct(StringComparer.Ordinal).Count();
		var result = new ConfigurationFixCollectionResult(
			workingDirectory,
			proposals,
			diagnosticCount,
			fixableDiagnosticCount,
			Math.Max(0, diagnosticCount - fixableDiagnosticCount));

		return result;
	}

	private static async Task<ImmutableArray<ResolvedConfigurationFix>> CollectResolvedConfigurationFixesAsync(Project project, ProjectAnalysisResult analysis, CancellationToken cancellationToken)
	{
		var collected = await ConfigurationFixProposalCollector.CollectAsync(project, analysis.AnalyzerDiagnostics, analysis.InlineConfigSourcePath, cancellationToken);
		var builder = ImmutableArray.CreateBuilder<ResolvedConfigurationFix>();
		foreach (var proposal in collected)
		{
			builder.Add(new ResolvedConfigurationFix(MapProposal(proposal.Proposal), proposal.DiagnosticKey, proposal.OriginalSolution, proposal.ChangedSolution));
		}

		var result = builder.ToImmutable();

		return result;
	}
	private static ConfigurationFixProposal MapProposal(ConfigurationFixChangeProposal proposal)
	{
		var result = new ConfigurationFixProposal(
			proposal.Id,
			proposal.ProjectPath,
			proposal.ProjectName,
			proposal.DiagnosticId,
			proposal.DiagnosticMessage,
			proposal.Title,
			MapRisk(proposal.Risk),
			proposal.TargetPath,
			proposal.PreviewDiff,
			proposal.ChangedPaths,
			proposal.DiagnosticProperties);

		return result;
	}

	private static ConfigurationFixRisk MapRisk(ConfigurationFixRiskLevel risk)
	{
		var result = risk switch
		{
			ConfigurationFixRiskLevel.Safe => ConfigurationFixRisk.Safe,
			ConfigurationFixRiskLevel.HighRisk => ConfigurationFixRisk.HighRisk,
			_ => ConfigurationFixRisk.Guided
		};

		return result;
	}

	private sealed record ResolvedConfigurationFix(
		ConfigurationFixProposal Proposal,
		string DiagnosticKey,
		Solution OriginalSolution,
		Solution ChangedSolution);
}
