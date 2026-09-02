using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Diagnostics.CodeFixes;

internal static class ConfigurationFixProposalCollector
{
	internal static async Task<ImmutableArray<ResolvedConfigurationFixProposal>> CollectAsync(
		Project project,
		ImmutableArray<Diagnostic> analyzerDiagnostics,
		string? inlineConfigSourcePath,
		CancellationToken cancellationToken)
	{
		var provider = new ArchitecturalLevelCodeFixProvider();
		var builder = ImmutableArray.CreateBuilder<ResolvedConfigurationFixProposal>();
		var seenProposalIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var diagnostic in analyzerDiagnostics.OrderBy(GetDiagnosticSortKey, StringComparer.Ordinal))
		{
			var document = FindDiagnosticDocument(project, diagnostic);
			if (document is null)
			{
				continue;
			}

			var actions = new List<CodeAction>();
			var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken);
			await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

			foreach (var action in actions)
			{
				var operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
				var applyOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
				if (applyOperation is null)
				{
					continue;
				}

				var preview = await TryCreatePreviewAsync(project.Solution, applyOperation.ChangedSolution, inlineConfigSourcePath, cancellationToken).ConfigureAwait(false);
				if (preview is null)
				{
					continue;
				}

				var proposal = new ConfigurationFixChangeProposal(
					BuildProposalId(project.FilePath ?? project.Name, diagnostic, action.Title),
					project.FilePath ?? string.Empty,
					project.Name,
					diagnostic.Id,
					diagnostic.GetMessage(CultureInfo.InvariantCulture),
					action.Title,
					ClassifyRisk(diagnostic.Id),
					preview.PrimaryPath,
					preview.Markdown,
					preview.ChangedPaths,
					ReadDiagnosticProperties(diagnostic));
				if (!seenProposalIds.Add(proposal.Id))
				{
					continue;
				}

				builder.Add(new ResolvedConfigurationFixProposal(proposal, BuildDiagnosticKey(project.FilePath ?? project.Name, diagnostic), project.Solution, applyOperation.ChangedSolution));
			}
		}

		var result = builder.ToImmutable();

		return result;
	}

	internal static async Task<ImmutableArray<string>> PersistChangesAsync(
		Solution originalSolution,
		Solution changedSolution,
		string? inlineConfigSourcePath,
		CancellationToken cancellationToken)
	{
		var preview = await TryCreatePreviewAsync(originalSolution, changedSolution, inlineConfigSourcePath, cancellationToken).ConfigureAwait(false)
		              ?? throw new InvalidOperationException("The selected code action did not produce a configuration-only change.");

		foreach (var change in preview.Changes)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var directory = Path.GetDirectoryName(change.Path);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			await Task.Run(() => File.WriteAllText(change.Path, change.UpdatedText, change.Encoding), cancellationToken).ConfigureAwait(false);
		}

		var result = preview.ChangedPaths;

		return result;
	}

	private static async Task<ConfigurationFixChangePreview?> TryCreatePreviewAsync(
		Solution originalSolution,
		Solution changedSolution,
		string? inlineConfigSourcePath,
		CancellationToken cancellationToken)
	{
		var solutionChanges = changedSolution.GetChanges(originalSolution);
		if (HasStructuralSolutionChanges(originalSolution, changedSolution))
		{
			return null;
		}

		var changes = ImmutableArray.CreateBuilder<ConfigurationTextChange>();
		foreach (var projectChanges in solutionChanges.GetProjectChanges())
		{
			if (HasStructuralProjectChanges(projectChanges))
			{
				return null;
			}

			foreach (var documentId in projectChanges.GetChangedAdditionalDocuments())
			{
				var originalDocument = originalSolution.GetAdditionalDocument(documentId);
				var changedDocument = changedSolution.GetAdditionalDocument(documentId);
				if (originalDocument?.FilePath is not { } path
				    || changedDocument is null
				    || !path.EndsWith(".anl", StringComparison.OrdinalIgnoreCase))
				{
					return null;
				}

				var originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
				var updatedText = await changedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
				if (string.Equals(originalText.ToString(), updatedText.ToString(), StringComparison.Ordinal))
				{
					continue;
				}

				changes.Add(new ConfigurationTextChange(path, originalText.ToString(), updatedText.ToString(), originalText.Encoding ?? new UTF8Encoding(false)));
			}

			foreach (var documentId in projectChanges.GetChangedDocuments())
			{
				var originalDocument = originalSolution.GetDocument(documentId);
				var changedDocument = changedSolution.GetDocument(documentId);
				if (originalDocument?.FilePath is not { } path
				    || changedDocument is null
				    || !string.Equals(path, inlineConfigSourcePath, StringComparison.OrdinalIgnoreCase))
				{
					return null;
				}

				var originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
				var updatedText = await changedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
				if (string.Equals(originalText.ToString(), updatedText.ToString(), StringComparison.Ordinal))
				{
					continue;
				}

				if (!IsInlineConfigurationOnlyChange(originalText.ToString(), updatedText.ToString()))
				{
					return null;
				}

				changes.Add(new ConfigurationTextChange(path, originalText.ToString(), updatedText.ToString(), originalText.Encoding ?? new UTF8Encoding(false)));
			}
		}

		if (changes.Count == 0)
		{
			return null;
		}

		var result = CreatePreview(changes.ToImmutable());

		return result;
	}

	private static bool HasStructuralSolutionChanges(Solution originalSolution, Solution changedSolution)
	{
		var originalProjectIds = originalSolution.ProjectIds.Select(id => id.Id.ToString()).OrderBy(id => id, StringComparer.Ordinal).ToArray();
		var changedProjectIds = changedSolution.ProjectIds.Select(id => id.Id.ToString()).OrderBy(id => id, StringComparer.Ordinal).ToArray();
		var result = !originalProjectIds.SequenceEqual(changedProjectIds);

		return result;
	}

	private static bool HasStructuralProjectChanges(ProjectChanges projectChanges)
	{
		var originalProject = projectChanges.OldProject;
		var changedProject = projectChanges.NewProject;
		if (!HasSameIds(originalProject.DocumentIds, changedProject.DocumentIds)
		    || !HasSameIds(originalProject.AdditionalDocumentIds, changedProject.AdditionalDocumentIds))
		{
			return true;
		}

		if (!HasSameTextPaths(originalProject.MetadataReferences.Select(GetReferenceDisplay), changedProject.MetadataReferences.Select(GetReferenceDisplay))
		    || !HasSameTextPaths(originalProject.ProjectReferences.Select(reference => reference.ProjectId.Id.ToString()), changedProject.ProjectReferences.Select(reference => reference.ProjectId.Id.ToString()))
		    || !HasSameTextPaths(originalProject.AnalyzerReferences.Select(reference => reference.FullPath ?? reference.Display ?? string.Empty), changedProject.AnalyzerReferences.Select(reference => reference.FullPath ?? reference.Display ?? string.Empty)))
		{
			return true;
		}

		var result = false;

		return result;
	}

	private static bool HasSameIds<T>(IEnumerable<T> originalIds, IEnumerable<T> changedIds) where T : notnull
	{
		var orderedOriginalIds = originalIds.OrderBy(id => id.ToString(), StringComparer.Ordinal).ToArray();
		var orderedChangedIds = changedIds.OrderBy(id => id.ToString(), StringComparer.Ordinal).ToArray();
		var result = orderedOriginalIds.SequenceEqual(orderedChangedIds);

		return result;
	}

	private static bool HasSameTextPaths(IEnumerable<string> originalValues, IEnumerable<string> changedValues)
	{
		var orderedOriginalValues = originalValues.OrderBy(value => value, StringComparer.Ordinal).ToArray();
		var orderedChangedValues = changedValues.OrderBy(value => value, StringComparer.Ordinal).ToArray();
		var result = orderedOriginalValues.SequenceEqual(orderedChangedValues, StringComparer.Ordinal);

		return result;
	}

	private static string GetReferenceDisplay(MetadataReference reference)
	{
		var result = reference.Display ?? string.Empty;

		return result;
	}

	private static ConfigurationFixChangePreview CreatePreview(ImmutableArray<ConfigurationTextChange> changes)
	{
		var markdown = new StringBuilder();
		markdown.AppendLine("# Preview");
		markdown.AppendLine();
		foreach (var change in changes)
		{
			markdown.AppendLine("## `" + Path.GetFileName(change.Path) + "`");
			markdown.AppendLine();
			markdown.AppendLine("```diff");
			markdown.Append(BuildDiff(change.Path, change.OriginalText, change.UpdatedText));
			markdown.AppendLine("```");
			markdown.AppendLine();
		}

		var result = new ConfigurationFixChangePreview(changes[0].Path, markdown.ToString().TrimEnd(), changes.Select(change => change.Path).ToImmutableArray(), changes);

		return result;
	}

	private static string BuildDiff(string path, string originalText, string updatedText)
	{
		var originalLines = SplitLines(originalText);
		var updatedLines = SplitLines(updatedText);
		var prefix = 0;
		while (prefix < originalLines.Length
		       && prefix < updatedLines.Length
		       && string.Equals(originalLines[prefix], updatedLines[prefix], StringComparison.Ordinal))
		{
			prefix++;
		}

		var suffix = 0;
		while (suffix < originalLines.Length - prefix
		       && suffix < updatedLines.Length - prefix
		       && string.Equals(originalLines[originalLines.Length - suffix - 1], updatedLines[updatedLines.Length - suffix - 1], StringComparison.Ordinal))
		{
			suffix++;
		}

		var startContext = Math.Max(0, prefix - 2);
		var endUpdated = Math.Min(updatedLines.Length, updatedLines.Length - suffix + 2);
		var builder = new StringBuilder();
		builder.AppendLine("--- " + path);
		builder.AppendLine("+++ " + path);
		builder.AppendLine("@@");
		for (var index = startContext; index < prefix; index++)
		{
			builder.AppendLine(" " + originalLines[index]);
		}

		for (var index = prefix; index < originalLines.Length - suffix; index++)
		{
			builder.AppendLine("-" + originalLines[index]);
		}

		for (var index = prefix; index < updatedLines.Length - suffix; index++)
		{
			builder.AppendLine("+" + updatedLines[index]);
		}

		for (var index = updatedLines.Length - suffix; index < endUpdated; index++)
		{
			builder.AppendLine(" " + updatedLines[index]);
		}

		var result = builder.ToString();

		return result;
	}

	private static string[] SplitLines(string value)
	{
		var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
		var result = normalized.Split('\n');

		return result;
	}

	private static bool IsInlineConfigurationOnlyChange(string originalSource, string updatedSource)
	{
		if (!InlineAssemblyMetadataSettings.TryFindInlineSettings(originalSource, out var settings, out _))
		{
			return false;
		}

		var prefix = 0;
		while (prefix < originalSource.Length
		       && prefix < updatedSource.Length
		       && originalSource[prefix] == updatedSource[prefix])
		{
			prefix++;
		}

		var suffix = 0;
		while (suffix < originalSource.Length - prefix
		       && suffix < updatedSource.Length - prefix
		       && originalSource[originalSource.Length - suffix - 1] == updatedSource[updatedSource.Length - suffix - 1])
		{
			suffix++;
		}

		var originalEnd = originalSource.Length - suffix;
		var result = prefix >= settings.LiteralSpan.Start
		             && originalEnd <= settings.LiteralSpan.End;

		return result;
	}

	private static Document? FindDiagnosticDocument(Project project, Diagnostic diagnostic)
	{
		if (diagnostic.Location != Location.None && diagnostic.Location.SourceTree is not null)
		{
			var treeDocument = project.Solution.GetDocument(diagnostic.Location.SourceTree);
			if (treeDocument is not null)
			{
				return treeDocument;
			}
		}

		var result = project.Documents.OrderBy(document => document.FilePath ?? document.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

		return result;
	}

	private static ConfigurationFixRiskLevel ClassifyRisk(string diagnosticId)
	{
		var result = diagnosticId switch
		{
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage => ConfigurationFixRiskLevel.HighRisk,
			ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure => ConfigurationFixRiskLevel.HighRisk,
			ArchitecturalDiagnosticIds.ProjectReferenceViolation => ConfigurationFixRiskLevel.HighRisk,
			ArchitecturalDiagnosticIds.PackageReferenceViolation => ConfigurationFixRiskLevel.HighRisk,
			ArchitecturalDiagnosticIds.VisibilityPolicyViolation => ConfigurationFixRiskLevel.HighRisk,
			ArchitecturalDiagnosticIds.CyclicDependencyGraph => ConfigurationFixRiskLevel.HighRisk,
			_ => ConfigurationFixRiskLevel.Guided
		};

		return result;
	}

	private static string GetDiagnosticSortKey(Diagnostic diagnostic)
	{
		var lineSpan = diagnostic.Location.GetLineSpan();
		var result = string.Join(
			"|",
			lineSpan.Path ?? string.Empty,
			lineSpan.StartLinePosition.Line.ToString(CultureInfo.InvariantCulture),
			lineSpan.StartLinePosition.Character.ToString(CultureInfo.InvariantCulture),
			diagnostic.Id,
			diagnostic.GetMessage(CultureInfo.InvariantCulture));

		return result;
	}

	private static string BuildDiagnosticKey(string projectPath, Diagnostic diagnostic)
	{
		var lineSpan = diagnostic.Location.GetLineSpan();
		var properties = string.Join(";", diagnostic.Properties.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value));
		var result = string.Join(
			"|",
			projectPath,
			diagnostic.Id,
			lineSpan.Path ?? string.Empty,
			lineSpan.StartLinePosition.Line.ToString(CultureInfo.InvariantCulture),
			lineSpan.StartLinePosition.Character.ToString(CultureInfo.InvariantCulture),
			diagnostic.GetMessage(CultureInfo.InvariantCulture),
			properties);

		return result;
	}

	private static string BuildProposalId(string projectPath, Diagnostic diagnostic, string title)
	{
		var raw = BuildDiagnosticKey(projectPath, diagnostic) + "|" + title;
		string hash;
		using (var sha256 = SHA256.Create())
		{
			var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
			hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).Substring(0, 12);
		}
		var result = "fix-" + hash.ToLowerInvariant();

		return result;
	}

	private static ImmutableDictionary<string, string> ReadDiagnosticProperties(Diagnostic diagnostic)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		foreach (var property in diagnostic.Properties)
		{
			if (string.IsNullOrWhiteSpace(property.Key) || string.IsNullOrWhiteSpace(property.Value))
			{
				continue;
			}

			builder[property.Key] = property.Value!;
		}

		var result = builder.ToImmutable();

		return result;
	}

	private sealed class ConfigurationTextChange
	{
		public ConfigurationTextChange(string path, string originalText, string updatedText, Encoding encoding)
		{
			Path = path;
			OriginalText = originalText;
			UpdatedText = updatedText;
			Encoding = encoding;
		}

		public string Path { get; }

		public string OriginalText { get; }

		public string UpdatedText { get; }

		public Encoding Encoding { get; }
	}

	private sealed class ConfigurationFixChangePreview
	{
		public ConfigurationFixChangePreview(
			string primaryPath,
			string markdown,
			ImmutableArray<string> changedPaths,
			ImmutableArray<ConfigurationTextChange> changes)
		{
			PrimaryPath = primaryPath;
			Markdown = markdown;
			ChangedPaths = changedPaths;
			Changes = changes;
		}

		public string PrimaryPath { get; }

		public string Markdown { get; }

		public ImmutableArray<string> ChangedPaths { get; }

		public ImmutableArray<ConfigurationTextChange> Changes { get; }
	}
}
