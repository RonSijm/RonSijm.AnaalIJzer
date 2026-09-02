using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Diagnostics.CodeFixes;

internal enum ConfigurationFixRiskLevel
{
	Safe,
	Guided,
	HighRisk
}

internal sealed class ConfigurationFixChangeProposal
{
	public ConfigurationFixChangeProposal(
		string id,
		string projectPath,
		string projectName,
		string diagnosticId,
		string diagnosticMessage,
		string title,
		ConfigurationFixRiskLevel risk,
		string targetPath,
		string previewDiff,
		ImmutableArray<string> changedPaths,
		ImmutableDictionary<string, string> diagnosticProperties)
	{
		Id = id;
		ProjectPath = projectPath;
		ProjectName = projectName;
		DiagnosticId = diagnosticId;
		DiagnosticMessage = diagnosticMessage;
		Title = title;
		Risk = risk;
		TargetPath = targetPath;
		PreviewDiff = previewDiff;
		ChangedPaths = changedPaths;
		DiagnosticProperties = diagnosticProperties;
	}

	public string Id { get; }

	public string ProjectPath { get; }

	public string ProjectName { get; }

	public string DiagnosticId { get; }

	public string DiagnosticMessage { get; }

	public string Title { get; }

	public ConfigurationFixRiskLevel Risk { get; }

	public string TargetPath { get; }

	public string PreviewDiff { get; }

	public ImmutableArray<string> ChangedPaths { get; }

	public ImmutableDictionary<string, string> DiagnosticProperties { get; }
}

internal sealed class ResolvedConfigurationFixProposal
{
	public ResolvedConfigurationFixProposal(
		ConfigurationFixChangeProposal proposal,
		string diagnosticKey,
		Solution originalSolution,
		Solution changedSolution)
	{
		Proposal = proposal;
		DiagnosticKey = diagnosticKey;
		OriginalSolution = originalSolution;
		ChangedSolution = changedSolution;
	}

	public ConfigurationFixChangeProposal Proposal { get; }

	public string DiagnosticKey { get; }

	public Solution OriginalSolution { get; }

	public Solution ChangedSolution { get; }
}
