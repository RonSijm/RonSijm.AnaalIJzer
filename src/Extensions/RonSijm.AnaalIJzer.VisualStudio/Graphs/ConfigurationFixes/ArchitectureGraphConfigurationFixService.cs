using System.IO;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;
using RonSijm.AnaalIJzer.Diagnostics.CodeFixes;
using RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;
using RonSijm.AnaalIJzer.Engine;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Graphs.ConfigurationFixes;

internal sealed class ArchitectureGraphConfigurationFixService
{
	private readonly VisualStudioWorkspace _workspace;

	internal ArchitectureGraphConfigurationFixService(VisualStudioWorkspace workspace)
	{
		_workspace = workspace;
	}

	internal static ArchitectureGraphConfigurationFixService Create()
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var componentModel = (IComponentModel?)Package.GetGlobalService(typeof(SComponentModel))
		                     ?? throw new InvalidOperationException("Visual Studio did not provide the MEF component model.");
		var workspace = componentModel.GetService<VisualStudioWorkspace>()
		               ?? throw new InvalidOperationException("Visual Studio did not provide the Roslyn workspace.");
		var result = new ArchitectureGraphConfigurationFixService(workspace);

		return result;
	}

	internal async Task<ArchitectureGraphConfigurationFixCollection> LoadAsync(ArchitectureGraphToolWindowContext context, CancellationToken cancellationToken)
	{
		var analysis = await AnalyzeAsync(context, cancellationToken);
		if (!string.IsNullOrWhiteSpace(analysis.Message))
		{
			return new ArchitectureGraphConfigurationFixCollection(analysis.Message ?? string.Empty, ImmutableArray<ArchitectureGraphConfigurationFixProposal>.Empty);
		}

		var message = "Loaded " + analysis.Proposals.Length + " configuration fix proposal(s).";
		if (analysis.Proposals.Length == 0)
		{
			message = analysis.DiagnosticCount == 0
				? "Architecture inspection found no analyzer diagnostics in the active project."
				: "Architecture inspection found " + analysis.DiagnosticCount + " diagnostic(s), but none exposed a configuration fix.";
		}

		var proposalBuilder = ImmutableArray.CreateBuilder<ArchitectureGraphConfigurationFixProposal>(analysis.Proposals.Length);
		foreach (var proposal in analysis.Proposals)
		{
			proposalBuilder.Add(MapProposal(proposal.Proposal));
		}

		var proposals = proposalBuilder.ToImmutable();
		var result = new ArchitectureGraphConfigurationFixCollection(message, proposals);

		return result;
	}

	internal async Task<ArchitectureGraphConfigurationFixApplyResult> ApplyAsync(ArchitectureGraphToolWindowContext context, string fixId, CancellationToken cancellationToken)
	{
		var analysis = await AnalyzeAsync(context, cancellationToken);
		if (!string.IsNullOrWhiteSpace(analysis.Message))
		{
			throw new InvalidOperationException(analysis.Message);
		}

		var proposal = analysis.Proposals.FirstOrDefault(candidate => string.Equals(candidate.Proposal.Id, fixId, StringComparison.Ordinal))
		              ?? throw new InvalidOperationException("Configuration fix not found: " + fixId);

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
		var appliedToWorkspace = _workspace.TryApplyChanges(proposal.ChangedSolution);
		ArchitectureVisualStudioLog.Info("Applying configuration fix '" + fixId + "' to Visual Studio workspace returned " + appliedToWorkspace + ".");

		var changedPaths = await ConfigurationFixProposalCollector.PersistChangesAsync(
			proposal.OriginalSolution,
			proposal.ChangedSolution,
			analysis.InlineConfigSourcePath,
			cancellationToken);
		var pathList = string.Join(", ", changedPaths.Select(Path.GetFileName));
		var message = "Applied configuration fix to " + changedPaths.Length + " file(s): " + pathList + ".";
		var result = new ArchitectureGraphConfigurationFixApplyResult(message);

		return result;
	}

	private async Task<ConfigurationFixAnalysisResult> AnalyzeAsync(ArchitectureGraphToolWindowContext context, CancellationToken cancellationToken)
	{
		if (!context.HasWorkspaceContext)
		{
			var detachedResult = new ConfigurationFixAnalysisResult(
				ImmutableArray<ResolvedConfigurationFixProposal>.Empty,
				"Configuration fixes are available when the graph comes from an active C# document in a loaded Visual Studio project.",
				0,
				null);

			return detachedResult;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
		var document = ResolveDocument(context);
		if (document is null)
		{
			var noDocumentResult = new ConfigurationFixAnalysisResult(
				ImmutableArray<ResolvedConfigurationFixProposal>.Empty,
				"The active document could not be resolved from the current Visual Studio workspace.",
				0,
				null);

			return noDocumentResult;
		}

		var additionalFiles = await ArchitectureSnapshotProvider.ResolveAdditionalFilesAsync(document, context.DocumentPath, cancellationToken);
		var compilation = await document.Project.GetCompilationAsync(cancellationToken);
		if (compilation is null)
		{
			var noCompilationResult = new ConfigurationFixAnalysisResult(
				ImmutableArray<ResolvedConfigurationFixProposal>.Empty,
				"Visual Studio could not compile the active project for architecture inspection.",
				0,
				null);

			return noCompilationResult;
		}

		var inlineConfigSourcePath = ArchitectureConfigurationSourceDiscovery.TryReadInlineConfigurationTextDocument(compilation, null, cancellationToken)?.Path;
		var analyzerOptions = new AnalyzerOptions(additionalFiles, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
		var diagnostics = await compilation
			.WithAnalyzers([new ArchitecturalLevelAnalyzer()], analyzerOptions)
			.GetAnalyzerDiagnosticsAsync(cancellationToken);
		var proposals = await ConfigurationFixProposalCollector.CollectAsync(document.Project, diagnostics, inlineConfigSourcePath, cancellationToken);
		var result = new ConfigurationFixAnalysisResult(proposals, null, diagnostics.Length, inlineConfigSourcePath);

		return result;
	}

	private Document? ResolveDocument(ArchitectureGraphToolWindowContext context)
	{
		var project = _workspace.CurrentSolution.Projects.FirstOrDefault(candidate =>
			string.Equals(candidate.FilePath, context.ProjectPath, StringComparison.OrdinalIgnoreCase));
		if (project is null)
		{
			return null;
		}

		var result = project.Documents.FirstOrDefault(candidate =>
			string.Equals(candidate.FilePath, context.DocumentPath, StringComparison.OrdinalIgnoreCase));
		if (result is not null)
		{
			return result;
		}

		result = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(context.DocumentPath!)
			.Select(id => _workspace.CurrentSolution.GetDocument(id))
			.FirstOrDefault(document => document is not null);

		return result;
	}

	private static ArchitectureGraphConfigurationFixProposal MapProposal(ConfigurationFixChangeProposal proposal)
	{
		var result = new ArchitectureGraphConfigurationFixProposal(
			proposal.Id,
			proposal.Title,
			proposal.DiagnosticMessage,
			MapRisk(proposal.Risk),
			proposal.DiagnosticId,
			proposal.TargetPath,
			proposal.PreviewDiff,
			proposal.DiagnosticProperties);

		return result;
	}

	private static string MapRisk(ConfigurationFixRiskLevel risk)
	{
		var result = risk switch
		{
			ConfigurationFixRiskLevel.Safe => "Safe",
			ConfigurationFixRiskLevel.HighRisk => "High risk",
			_ => "Guided"
		};

		return result;
	}

	private sealed class ConfigurationFixAnalysisResult
	{
		public ConfigurationFixAnalysisResult(
			ImmutableArray<ResolvedConfigurationFixProposal> proposals,
			string? message,
			int diagnosticCount,
			string? inlineConfigSourcePath)
		{
			Proposals = proposals;
			Message = message;
			DiagnosticCount = diagnosticCount;
			InlineConfigSourcePath = inlineConfigSourcePath;
		}

		public ImmutableArray<ResolvedConfigurationFixProposal> Proposals { get; }

		public string? Message { get; }

		public int DiagnosticCount { get; }

		public string? InlineConfigSourcePath { get; }
	}
}
