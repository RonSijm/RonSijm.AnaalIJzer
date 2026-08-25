using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Findings;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Violations;

/// <summary>
///     Facade for architectural violation report generation.
/// </summary>
internal static class ArchitecturalViolationReporter
{
	internal static ImmutableArray<ArchitectureFinding> CreateFindings(IEnumerable<Diagnostic> diagnostics)
	{
		var result = diagnostics
			.Select(diagnostic => ArchitectureFindingFactory.FromDiagnostic(diagnostic))
			.ToImmutableArray();

		return result;
	}

	internal static ImmutableArray<ArchitectureFinding> CreateFindings(IEnumerable<ViolationRecord> violations)
	{
		var result = violations
			.Select(violation => violation.ToArchitectureFinding())
			.ToImmutableArray();

		return result;
	}

	internal static string GenerateMarkdownReport(IEnumerable<Diagnostic> diagnostics, AnalyzerConfiguration config, string? assemblyName)
	{
		var result = GenerateMarkdownReport(diagnostics, config, assemblyName, "Assembly");

		return result;
	}

	internal static string GenerateMarkdownReport(IEnumerable<Diagnostic> diagnostics, AnalyzerConfiguration config, string? inputName, string inputLabel)
	{
		var records = diagnostics
			.Select(ViolationRecordFactory.TryCreate)
			.OfType<ViolationRecord>();
		var result = ViolationMarkdownReportBuilder.Generate(records, config, inputName, inputLabel);

		return result;
	}

	internal static string GenerateMarkdownReport(IEnumerable<ViolationRecord> violationBag, AnalyzerConfiguration config, string? assemblyName)
	{
		var result = GenerateMarkdownReport(violationBag, config, assemblyName, "Assembly");

		return result;
	}

	internal static string GenerateMarkdownReport(IEnumerable<ViolationRecord> violationBag, AnalyzerConfiguration config, string? inputName, string inputLabel)
	{
		var result = ViolationMarkdownReportBuilder.Generate(violationBag, config, inputName, inputLabel);

		return result;
	}
}
