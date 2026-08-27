using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine;

public sealed partial class ArchitecturalLevelAnalyzer
{
	private static void ReportConfigurationIssues(CompilationAnalysisContext context, AnalyzerConfig config, ImmutableArray<AdditionalText> additionalFiles)
	{
		foreach (var issue in config.ConfigurationIssues)
		{
			var descriptor = issue.Kind == ConfigurationIssueKind.CyclicDependencyGraph
				? ArchitecturalDiagnostics.CyclicDependencyGraph
				: ArchitecturalDiagnostics.InvalidConfiguration;
			context.ReportDiagnostic(Diagnostic.Create(descriptor, CreateConfigurationLocation(issue, additionalFiles, context.CancellationToken), issue.Message));
		}
	}

	private static void ReportExceptionReviews(CompilationAnalysisContext context, AnalyzerConfig config, ImmutableArray<AdditionalText> additionalFiles)
	{
		foreach (var review in config.ExceptionReviews)
		{
			var properties = ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertyExceptionMatcherKind, review.MatcherKind)
				.Add(ArchitecturalDiagnostics.PropertyExceptionMatcherLabel, review.MatcherLabel)
				.Add(ArchitecturalDiagnostics.PropertyExceptionReason, review.Metadata.Reason)
				.Add(ArchitecturalDiagnostics.PropertyExceptionOwner, review.Metadata.Owner)
				.Add(ArchitecturalDiagnostics.PropertyExceptionExpiresOn, review.Metadata.ExpiresOnText)
				.Add(ArchitecturalDiagnostics.PropertyExceptionStatus, review.Status.ToString())
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, review.XmlPath)
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, review.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, review.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));
			context.ReportDiagnostic(Diagnostic.Create(
				ArchitecturalDiagnostics.ExceptionReview,
				CreateConfigurationLocation(review.XmlPath, review.XmlLineNumber, review.XmlLinePosition, additionalFiles, context.Compilation, context.CancellationToken),
				properties,
				review.Message));
		}
	}
}
