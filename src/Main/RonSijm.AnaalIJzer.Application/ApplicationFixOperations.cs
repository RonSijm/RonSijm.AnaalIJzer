using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Workspace.Analysis.ConfigurationFixes;

namespace RonSijm.AnaalIJzer.Application;

internal static class ApplicationFixOperations
{
	public static async Task<ApplicationRunResult> FindConfigurationFixesAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		var result = request.InputKind == ApplicationInputKind.Solution
			? await workspace.FindSolutionConfigurationFixesAsync(request, cancellationToken)
			: await workspace.FindProjectConfigurationFixesAsync(request, cancellationToken);
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(
			request.OutputPath,
			Path.Combine(result.WorkingDirectory, "architecture-fixes.md"),
			result.WorkingDirectory);
		var content = BuildMarkdown(request, result);
		if (request.WriteOutput)
		{
			await ApplicationOutputPathService.WriteOutputAsync(outputPath, content, request.Force, cancellationToken);
		}

		var message = result.Proposals.Length == 0
			? "No configuration fix proposals were found."
			: $"Found {result.Proposals.Length} configuration fix proposal(s) for {result.FixableDiagnosticCount} diagnostic(s).";
		if (request.WriteOutput)
		{
			message += $" Wrote {outputPath}";
		}

		var toolResult = new ApplicationRunResult(
			outputPath,
			message,
			result.Proposals.Length > 0,
			content,
			Findings: default,
			FixProposals: MapProposals(result.Proposals));

		return toolResult;
	}

	public static async Task<ApplicationRunResult> ApplyConfigurationFixAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		var applyResult = request.InputKind == ApplicationInputKind.Solution
			? await workspace.ApplySolutionConfigurationFixAsync(request, cancellationToken)
			: await workspace.ApplyProjectConfigurationFixAsync(request, cancellationToken);
		var remainingResult = request.InputKind == ApplicationInputKind.Solution
			? await workspace.FindSolutionConfigurationFixesAsync(request with { Operation = ApplicationOperationKind.Fixes, FixId = null }, cancellationToken)
			: await workspace.FindProjectConfigurationFixesAsync(request with { Operation = ApplicationOperationKind.Fixes, FixId = null }, cancellationToken);
		var changedFiles = string.Join(", ", applyResult.ChangedPaths.Select(Path.GetFileName));
		var message = $"Applied {applyResult.Proposal.Title}. Updated {changedFiles}. {remainingResult.Proposals.Length} configuration fix proposal(s) remain.";
		var outputPath = remainingResult.ChangedOutputPathOrDefault(request.OutputPath);
		var toolResult = new ApplicationRunResult(
			outputPath,
			message,
			remainingResult.Proposals.Length > 0,
			BuildMarkdown(request with { Operation = ApplicationOperationKind.Fixes, FixId = null }, remainingResult),
			Findings: default,
			FixProposals: MapProposals(remainingResult.Proposals));

		return toolResult;
	}

	private static ImmutableArray<ApplicationConfigurationFixProposal> MapProposals(ImmutableArray<ConfigurationFixProposal> proposals)
	{
		var result = proposals.Select(proposal => new ApplicationConfigurationFixProposal(
			proposal.Id,
			proposal.DiagnosticId,
			proposal.Title,
			proposal.Risk.ToString(),
			proposal.ProjectName,
			proposal.TargetPath,
			proposal.DiagnosticMessage,
			proposal.PreviewDiff,
			proposal.DiagnosticProperties)).ToImmutableArray();

		return result;
	}

	private static string BuildMarkdown(ApplicationRequest request, ConfigurationFixCollectionResult result)
	{
		var builder = new StringBuilder();
		builder.AppendLine("# Architecture Configuration Fixes");
		builder.AppendLine();
		builder.AppendLine("- **Input**: " + FormatInputLabel(request));
		builder.AppendLine("- **Diagnostics scanned**: " + result.DiagnosticCount);
		builder.AppendLine("- **Diagnostics with config fixes**: " + result.FixableDiagnosticCount);
		builder.AppendLine("- **Diagnostics without config fixes**: " + result.UnfixableDiagnosticCount);
		builder.AppendLine("- **Proposals**: " + result.Proposals.Length);
		builder.AppendLine();

		if (result.Proposals.Length == 0)
		{
			builder.AppendLine("No configuration-backed fix proposals are currently available for this input.");

			return builder.ToString().TrimEnd();
		}

		foreach (var proposal in result.Proposals)
		{
			builder.AppendLine("## `" + proposal.Id + "` - " + proposal.Title);
			builder.AppendLine();
			builder.AppendLine("- **Risk**: " + proposal.Risk);
			builder.AppendLine("- **Diagnostic**: `" + proposal.DiagnosticId + "`");
			builder.AppendLine("- **Project**: `" + proposal.ProjectName + "`");
			builder.AppendLine("- **Target**: `" + proposal.TargetPath + "`");
			builder.AppendLine("- **Reason**: " + proposal.DiagnosticMessage);
			builder.AppendLine();
			builder.AppendLine(proposal.PreviewDiff);
			builder.AppendLine();
		}

		var markdown = builder.ToString().TrimEnd();

		return markdown;
	}

	private static string FormatInputLabel(ApplicationRequest request)
	{
		var kind = request.InputKind == ApplicationInputKind.Solution ? "Solution" : "Project";
		var input = request.InputPaths.Count == 0 ? string.Empty : Path.GetFileName(request.InputPaths[0]);
		var result = $"`{kind}` `{input}`";

		return result;
	}
}

internal static class ConfigurationFixCollectionResultExtensions
{
	internal static string ChangedOutputPathOrDefault(this ConfigurationFixCollectionResult result, string? outputPath)
	{
		var resolved = ApplicationOutputPathService.ResolveOutputPath(outputPath, Path.Combine(result.WorkingDirectory, "architecture-fixes.md"), result.WorkingDirectory);

		return resolved;
	}
}
