using System.Text;
using System.Text.RegularExpressions;
using RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class DiagnosticIdMigrationOperations
{
	private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".cs", ".csproj", ".props", ".targets", ".ruleset", ".config", ".json", ".md", ".ps1", ".yml", ".yaml"
	};

	private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
	{
		".git", ".vs", "bin", "obj", "Artifacts", "TestResults"
	};

	public static async Task<ApplicationRunResult> GenerateReportAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var inputPath = Path.GetFullPath(request.InputPaths[0]);
		var rootDirectory = Path.GetDirectoryName(inputPath) ?? throw new ApplicationOperationException($"Cannot determine the directory for {inputPath}.");
		var findings = Scan(rootDirectory, cancellationToken);
		var report = BuildReport(rootDirectory, findings);
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(rootDirectory, "diagnostic-id-migration.md"), rootDirectory);
		await ApplicationOutputPathService.WriteOutputAsync(outputPath, report, request.Force, cancellationToken);
		var message = findings.Count == 0
			? $"No obsolete diagnostic IDs were found. Wrote {outputPath}"
			: $"Found {findings.Count} obsolete diagnostic ID occurrence(s). Wrote {outputPath}";
		var result = new ApplicationRunResult(outputPath, message, findings.Count > 0, report);

		return result;
	}

	private static List<DiagnosticIdMigrationFinding> Scan(string rootDirectory, CancellationToken cancellationToken)
	{
		var result = new List<DiagnosticIdMigrationFinding>();
		foreach (var filePath in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!ShouldScan(rootDirectory, filePath))
			{
				continue;
			}

			var lineNumber = 0;
			foreach (var line in File.ReadLines(filePath))
			{
				lineNumber++;
				foreach (Match match in LegacyDiagnosticIdRegex().Matches(line))
				{
					if (ArchitectureDiagnosticIdMigrationCatalog.TryGet(match.Value, out var migration))
					{
						result.Add(new DiagnosticIdMigrationFinding(Path.GetRelativePath(rootDirectory, filePath), lineNumber, migration!));
					}
				}
			}
		}

		return result;
	}

	private static bool ShouldScan(string rootDirectory, string filePath)
	{
		var relativePath = Path.GetRelativePath(rootDirectory, filePath);
		var pathSegments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
		if (pathSegments.Any(ExcludedDirectories.Contains))
		{
			return false;
		}

		var fileName = Path.GetFileName(filePath);
		var result = string.Equals(fileName, ".editorconfig", StringComparison.OrdinalIgnoreCase)
		             || SupportedExtensions.Contains(Path.GetExtension(filePath));

		return result;
	}

	private static string BuildReport(string rootDirectory, IReadOnlyList<DiagnosticIdMigrationFinding> findings)
	{
		var builder = new StringBuilder();
		builder.AppendLine("# Diagnostic ID Migration Report");
		builder.AppendLine();
		builder.AppendLine($"**Scanned root**: `{rootDirectory}`");
		builder.AppendLine();
		builder.AppendLine("This report is read-only. Arse did not modify source files, project properties, pragmas, rulesets, or `.editorconfig` entries.");
		builder.AppendLine();
		if (findings.Count == 0)
		{
			builder.AppendLine("No obsolete diagnostic IDs were found.");
			return ApplicationOutputPathService.EnsureFinalNewLine(builder.ToString());
		}

		builder.AppendLine("| File | Line | Old ID | Replacement | Action |");
		builder.AppendLine("| --- | ---: | --- | --- | --- |");
		foreach (var finding in findings.OrderBy(finding => finding.RelativePath, StringComparer.OrdinalIgnoreCase).ThenBy(finding => finding.LineNumber))
		{
			var replacements = string.Join(", ", finding.Migration.NewIds.Select(id => $"`{id}`"));
			var action = finding.Migration.HasAutomaticReplacement ? "Direct replacement" : finding.Migration.Note;
			builder.AppendLine($"| `{finding.RelativePath.Replace('\\', '/')}` | {finding.LineNumber} | `{finding.Migration.OldId}` | {replacements} | {action} |");
		}

		return ApplicationOutputPathService.EnsureFinalNewLine(builder.ToString());
	}

	[GeneratedRegex(@"(?<![A-Z0-9_])(?:ARCH|TOPO)[0-9]{3}(?![A-Z0-9_])", RegexOptions.CultureInvariant)]
	private static partial Regex LegacyDiagnosticIdRegex();

	private sealed record DiagnosticIdMigrationFinding(string RelativePath, int LineNumber, ArchitectureDiagnosticIdMigration Migration);
}
