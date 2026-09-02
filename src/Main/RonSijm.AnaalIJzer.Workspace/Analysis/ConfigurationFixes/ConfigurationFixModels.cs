using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Workspace.Analysis.ConfigurationFixes;

internal enum ConfigurationFixRisk
{
	Safe,
	Guided,
	HighRisk
}

internal sealed record ConfigurationFixProposal(
	string Id,
	string ProjectPath,
	string ProjectName,
	string DiagnosticId,
	string DiagnosticMessage,
	string Title,
	ConfigurationFixRisk Risk,
	string TargetPath,
	string PreviewDiff,
	ImmutableArray<string> ChangedPaths,
	ImmutableDictionary<string, string> DiagnosticProperties);

internal sealed record ConfigurationFixCollectionResult(
	string WorkingDirectory,
	ImmutableArray<ConfigurationFixProposal> Proposals,
	int DiagnosticCount,
	int FixableDiagnosticCount,
	int UnfixableDiagnosticCount);

internal sealed record ConfigurationFixApplyResult(
	ConfigurationFixProposal Proposal,
	ImmutableArray<string> ChangedPaths);
