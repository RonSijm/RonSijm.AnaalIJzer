using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using RonSijm.AnaalIJzer.Parsing;
using RonSijm.AnaalIJzer.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Snapshots;

[Export]
internal sealed class ArchitectureSnapshotProvider
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
		var configFingerprint = CreateConfigFingerprint(document.Project, textDocument.FilePath);
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
			return CreateSnapshotCoreAsync(documentId, buffer.CurrentSnapshot, includeCodeEvidence, cancellationToken);
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

	private async Task<ArchitectureEditorSnapshot> CreateSnapshotCoreAsync(DocumentId documentId, ITextSnapshot snapshot, bool includeCodeEvidence, CancellationToken cancellationToken)
	{
		var document = workspace.CurrentSolution.GetDocument(documentId);
		if (document is null)
		{
			ArchitectureVisualStudioLog.Warning("CreateSnapshotCoreAsync could not resolve Roslyn document id '" + documentId.Id + "'.");
			return ArchitectureEditorSnapshot.Empty;
		}

		document = document.WithText(SourceText.From(snapshot.GetText()));
		var additionalFiles = AddDiscoveredConfigurationFile(document.Project.AnalyzerOptions.AdditionalFiles, document.FilePath);
		ArchitectureVisualStudioLog.Info("Snapshot core using " + additionalFiles.Length + " additional file(s).");
		var result = await ArchitectureEditorSnapshotService.CreateSnapshotAsync(document, additionalFiles, includeCodeEvidence, cancellationToken);

		return result;
	}

	private static ImmutableArray<AdditionalText> AddDiscoveredConfigurationFile(ImmutableArray<AdditionalText> additionalFiles, string? documentPath)
	{
		if (documentPath is null)
		{
			return additionalFiles;
		}

		var discoveredPath = FindNearestArchitectureConfig(documentPath);
		if (discoveredPath is null || additionalFiles.Any(file => string.Equals(file.Path, discoveredPath, StringComparison.OrdinalIgnoreCase)))
		{
			ArchitectureVisualStudioLog.Info(discoveredPath is null
				? "No nearest architecture config fallback found for '" + documentPath + "'."
				: "Nearest architecture config fallback already present: '" + discoveredPath + "'.");
			return additionalFiles;
		}

		ArchitectureVisualStudioLog.Info("Adding nearest architecture config fallback: '" + discoveredPath + "'.");
		var result = additionalFiles.Add(new PhysicalAdditionalText(discoveredPath));

		return result;
	}

	private DocumentId? FindDocumentId(string filePath)
	{
		var result = workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
		if (result is not null)
		{
			return result;
		}

		result = workspace.CurrentSolution.Projects
			.SelectMany(project => project.Documents)
			.Where(document => string.Equals(document.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
			.Select(document => document.Id)
			.FirstOrDefault();

		return result;
	}

	private static string CreateConfigFingerprint(Project project, string documentPath)
	{
		var builder = new StringBuilder();
		builder.Append(project.FilePath ?? project.Name);
		builder.Append('|');
		builder.Append(project.Version.GetHashCode());
		foreach (var additionalFile in project.AnalyzerOptions.AdditionalFiles.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase))
		{
			builder.Append('|');
			builder.Append(additionalFile.Path);
			builder.Append(':');
			try
			{
				if (File.Exists(additionalFile.Path))
				{
					var info = new FileInfo(additionalFile.Path);
					builder.Append(info.Length);
					builder.Append('@');
					builder.Append(info.LastWriteTimeUtc.Ticks);
				}
				else
				{
					var text = additionalFile.GetText();
					builder.Append(text?.Length ?? 0);
					builder.Append('@');
					builder.Append(text?.ChecksumAlgorithm.ToString() ?? "none");
				}
			}
			catch (IOException)
			{
				builder.Append("unavailable");
			}
			catch (UnauthorizedAccessException)
			{
				builder.Append("unavailable");
			}
		}

		AppendFileFingerprint(builder, FindNearestArchitectureConfig(documentPath));

		var result = builder.ToString();

		return result;
	}

	private static string? FindNearestArchitectureConfig(string documentPath)
	{
		var directory = Path.GetDirectoryName(documentPath);
		while (!string.IsNullOrWhiteSpace(directory))
		{
			var candidate = Path.Combine(directory, ArchitecturalConfigParser.ConfigFileName);
			if (File.Exists(candidate))
			{
				return candidate;
			}

			var parent = Directory.GetParent(directory);
			if (parent is null)
			{
				break;
			}

			directory = parent.FullName;
		}

		return null;
	}

	private static void AppendFileFingerprint(StringBuilder builder, string? path)
	{
		if (path is null)
		{
			return;
		}

		builder.Append('|');
		builder.Append(path);
		builder.Append(':');
		try
		{
			var info = new FileInfo(path);
			builder.Append(info.Length);
			builder.Append('@');
			builder.Append(info.LastWriteTimeUtc.Ticks);
		}
		catch (IOException)
		{
			builder.Append("unavailable");
		}
		catch (UnauthorizedAccessException)
		{
			builder.Append("unavailable");
		}
	}

	private readonly struct SnapshotCacheKey(
        Guid documentId,
        int versionNumber,
        int projectVersion,
        string configFingerprint,
        bool includeCodeEvidence)
        : IEquatable<SnapshotCacheKey>
    {
        public Guid DocumentId { get; } = documentId;

        public int VersionNumber { get; } = versionNumber;

        public int ProjectVersion { get; } = projectVersion;

        public string ConfigFingerprint { get; } = configFingerprint;

        public bool IncludeCodeEvidence { get; } = includeCodeEvidence;

        public bool Equals(SnapshotCacheKey other)
		{
			var result = DocumentId.Equals(other.DocumentId)
			             && VersionNumber == other.VersionNumber
			             && ProjectVersion == other.ProjectVersion
			             && string.Equals(ConfigFingerprint, other.ConfigFingerprint, StringComparison.Ordinal)
			             && IncludeCodeEvidence == other.IncludeCodeEvidence;

			return result;
		}

		public override bool Equals(object? obj)
		{
			var result = obj is SnapshotCacheKey other && Equals(other);

			return result;
		}

		public override int GetHashCode()
		{
			var result = unchecked((((DocumentId.GetHashCode() * 397) ^ VersionNumber) * 397) ^ ProjectVersion);
			result = unchecked((result * 397) ^ StringComparer.Ordinal.GetHashCode(ConfigFingerprint));
			result = unchecked((result * 397) ^ IncludeCodeEvidence.GetHashCode());

			return result;
		}
	}

	private sealed class PhysicalAdditionalText(string path) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
		{
			var result = SourceText.From(File.ReadAllText(Path), Encoding.UTF8);

			return result;
		}
	}
}
