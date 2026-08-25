using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.EditorRuntime.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;

[Export]
internal sealed partial class ArchitectureSnapshotProvider
{
	private readonly VisualStudioWorkspace workspace;
	private readonly ITextDocumentFactoryService textDocumentFactory;
	private readonly ConcurrentDictionary<SnapshotCacheKey, Task<ArchitectureEditorSnapshot>> cache = new();
	private const int MaximumCachedSnapshots = 128;

	[System.ComponentModel.Composition.ImportingConstructor]
	public ArchitectureSnapshotProvider(VisualStudioWorkspace workspace, ITextDocumentFactoryService textDocumentFactory)
	{
		this.workspace = workspace;
		this.textDocumentFactory = textDocumentFactory;
		this.workspace.WorkspaceChanged += (_, args) =>
		{
			cache.Clear();
			ArchitectureVisualStudioLog.Info("Workspace changed: " + args.Kind + ". Snapshot cache cleared.");
		};
		ArchitectureVisualStudioLog.Info("ArchitectureSnapshotProvider created.");
	}

	internal async Task<ArchitectureEditorSnapshot> CreateSnapshotAsync(ITextBuffer buffer, CancellationToken cancellationToken)
	{
		if (!textDocumentFactory.TryGetTextDocument(buffer, out var textDocument))
		{
			ArchitectureVisualStudioDiagnostics.Publish("AnaalIJzer did not analyze the active buffer because Visual Studio did not expose it as a text document.");
			return ArchitectureEditorSnapshot.Empty;
		}

		ArchitectureVisualStudioLog.Info("Creating architecture snapshot for '" + textDocument.FilePath + "'.");
		var documentId = FindDocumentId(textDocument.FilePath);
		if (documentId is null)
		{
			ArchitectureVisualStudioDiagnostics.Publish($"AnaalIJzer did not analyze '{textDocument.FilePath}' because the file is not part of the current Roslyn workspace.");
			ArchitectureVisualStudioLog.Warning("No Roslyn document id found for '" + textDocument.FilePath + "'.");
			return ArchitectureEditorSnapshot.Empty;
		}

		var document = workspace.CurrentSolution.GetDocument(documentId);
		if (document is null)
		{
			ArchitectureVisualStudioDiagnostics.Publish($"AnaalIJzer did not analyze '{textDocument.FilePath}' because Visual Studio could not resolve the Roslyn document.");
			ArchitectureVisualStudioLog.Warning("Roslyn document id '" + documentId.Id + "' resolved no document for '" + textDocument.FilePath + "'.");
			return ArchitectureEditorSnapshot.Empty;
		}

		ArchitectureVisualStudioLog.Info(
			"Resolved Roslyn document. Project='"
			+ document.Project.Name
			+ "', ProjectPath='"
			+ (document.Project.FilePath ?? "<none>")
			+ "', AdditionalFiles="
			+ document.Project.AnalyzerOptions.AdditionalFiles.Length
			+ ".");
		var versionNumber = buffer.CurrentSnapshot.Version.VersionNumber;
		var projectVersion = document.Project.Version.GetHashCode();
		var includeCodeEvidence = ArchitectureVisualStudioOptions.Current.IncludeCodeEvidenceInDependencyGraphs;
		var additionalFiles = await ResolveAdditionalFilesAsync(document, textDocument.FilePath, cancellationToken);
		var configFingerprint = CreateConfigFingerprint(document.Project, additionalFiles);
		var key = new SnapshotCacheKey(documentId.Id, versionNumber, projectVersion, configFingerprint, includeCodeEvidence);
		if (cache.Count > MaximumCachedSnapshots)
		{
			cache.Clear();
			ArchitectureVisualStudioLog.Info("Snapshot cache exceeded " + MaximumCachedSnapshots + " entries and was cleared.");
		}

		var createdTask = false;
		var task = cache.GetOrAdd(key, _ =>
		{
			createdTask = true;
			return CreateSnapshotCoreAsync(documentId, buffer.CurrentSnapshot, includeCodeEvidence, additionalFiles, cancellationToken);
		});
		ArchitectureVisualStudioLog.Info(createdTask ? "Snapshot cache miss; analyzing document." : "Snapshot cache hit.");
		try
		{
			var result = await task;
			ArchitectureVisualStudioDiagnostics.Publish(ArchitectureVisualStudioDiagnostics.FormatSnapshot(textDocument.FilePath, result));
			ArchitectureVisualStudioLog.Info(
				"Snapshot completed for '"
				+ textDocument.FilePath
				+ "'. HasConfiguration="
				+ result.HasConfiguration
				+ ", HasConfigurationIssues="
				+ result.HasConfigurationIssues
				+ ", Layers="
				+ result.LayerIndicators.Length
				+ ", Sites="
				+ result.SiteIndicators.Length
				+ ".");

			return result;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			cache.TryRemove(key, out _);
			throw;
		}
	}

	private async Task<ArchitectureEditorSnapshot> CreateSnapshotCoreAsync(DocumentId documentId, ITextSnapshot snapshot, bool includeCodeEvidence, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		var document = workspace.CurrentSolution.GetDocument(documentId);
		if (document is null)
		{
			ArchitectureVisualStudioLog.Warning("CreateSnapshotCoreAsync could not resolve Roslyn document id '" + documentId.Id + "'.");
			return ArchitectureEditorSnapshot.Empty;
		}

		document = document.WithText(SourceText.From(snapshot.GetText()));
		ArchitectureVisualStudioLog.Info("Snapshot core using " + additionalFiles.Length + " additional file(s).");
		var result = await ArchitectureEditorSnapshotService.CreateSnapshotAsync(document, additionalFiles, includeCodeEvidence, cancellationToken);

		return result;
	}
}
