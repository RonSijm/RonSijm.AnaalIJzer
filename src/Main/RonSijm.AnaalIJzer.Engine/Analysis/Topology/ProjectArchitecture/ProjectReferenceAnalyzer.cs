using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.Topology.ProjectArchitecture;

internal static class ProjectReferenceAnalyzer
{
	internal static void AnalyzeCompilation(CompilationAnalysisContext context, AnalyzerConfiguration config, ImmutableArray<AdditionalText> additionalFiles)
	{
		if (!config.HasProjectArchitecture)
		{
			return;
		}

		var manifestFile = additionalFiles.FirstOrDefault(file => string.Equals(Path.GetFileName(file.Path), ArchitectureReferenceManifest.FileName, StringComparison.OrdinalIgnoreCase));
		if (manifestFile is null)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				ArchitecturalDiagnostics.InvalidConfiguration,
				Location.None,
				"ProjectArchitecture is configured but no project-reference manifest was supplied. Ensure the build integration adds the AnaalIJzer reference manifest as an AdditionalFile."));
			return;
		}

		var manifestText = manifestFile.GetText(context.CancellationToken)?.ToString();
		if (string.IsNullOrWhiteSpace(manifestText))
		{
			context.ReportDiagnostic(Diagnostic.Create(
				ArchitecturalDiagnostics.InvalidConfiguration,
				Location.None,
				$"ProjectArchitecture is configured but the project-reference manifest '{manifestFile.Path}' is empty."));
			return;
		}

		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();
		var manifest = ArchitectureReferenceManifestReader.Read(manifestText!, manifestFile.Path, issues);
		foreach (var issue in issues)
		{
			context.ReportDiagnostic(Diagnostic.Create(ArchitecturalDiagnostics.InvalidConfiguration, Location.None, issue.Message));
		}

		var analysis = ProjectArchitectureAnalysisService.Analyze(config.ProjectArchitecture, manifest);
		foreach (var projectReference in analysis.ProjectReferenceViolations)
		{
			var sourceGroup = projectReference.SourceProjectGroup ?? "unrecognized";
			var targetGroup = projectReference.TargetProjectGroup ?? "unrecognized";
			var properties = ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertySourceProjectPath, projectReference.SourceProjectPath)
				.Add(ArchitecturalDiagnostics.PropertySourceProjectName, projectReference.SourceProjectName)
				.Add(ArchitecturalDiagnostics.PropertySourceProjectGroup, sourceGroup)
				.Add(ArchitecturalDiagnostics.PropertyTargetProjectPath, projectReference.TargetProjectPath)
				.Add(ArchitecturalDiagnostics.PropertyTargetProjectName, projectReference.TargetProjectName)
				.Add(ArchitecturalDiagnostics.PropertyTargetProjectGroup, targetGroup)
				.Add(ArchitecturalDiagnostics.PropertyViolationReason, projectReference.ViolationReason);

			if (projectReference.MatchedRule is { } matchedRule)
			{
				properties = properties
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, matchedRule.XmlPath)
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, matchedRule.XmlLineNumber.ToString())
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, matchedRule.XmlLinePosition.ToString());
			}

			var diagnostic = Diagnostic.Create(
				ArchitecturalDiagnostics.ProjectReferenceViolation,
				Location.None,
				properties,
				projectReference.SourceProjectName,
				sourceGroup,
				projectReference.TargetProjectName,
				targetGroup,
				projectReference.ViolationReason);

			context.ReportDiagnostic(diagnostic);
		}

		foreach (var packageReference in analysis.PackageReferenceViolations)
		{
			var sourceGroup = packageReference.SourceProjectGroup ?? "unrecognized";
			var properties = ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertySourceProjectPath, packageReference.SourceProjectPath)
				.Add(ArchitecturalDiagnostics.PropertySourceProjectName, packageReference.SourceProjectName)
				.Add(ArchitecturalDiagnostics.PropertySourceProjectGroup, sourceGroup)
				.Add(ArchitecturalDiagnostics.PropertyPackageId, packageReference.PackageId)
				.Add(ArchitecturalDiagnostics.PropertyPackageVersion, packageReference.PackageVersion)
				.Add(ArchitecturalDiagnostics.PropertyPackageReferenceKind, packageReference.ReferenceKind.ToString())
				.Add(ArchitecturalDiagnostics.PropertyViolationReason, packageReference.ViolationReason);

			if (packageReference.MatchedPolicy is { } matchedPolicy)
			{
				properties = properties
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, matchedPolicy.XmlPath)
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, matchedPolicy.XmlLineNumber.ToString())
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, matchedPolicy.XmlLinePosition.ToString());
			}

			if (packageReference.MatchedMatcher is { } matchedMatcher && !string.IsNullOrWhiteSpace(matchedMatcher.Comment))
			{
				properties = properties.Add(ArchitecturalDiagnostics.PropertyComment, matchedMatcher.Comment);
			}

			var diagnostic = Diagnostic.Create(
				ArchitecturalDiagnostics.PackageReferenceViolation,
				Location.None,
				properties,
				packageReference.SourceProjectName,
				sourceGroup,
				packageReference.PackageId,
				packageReference.PackageVersion,
				packageReference.ViolationReason);

			context.ReportDiagnostic(diagnostic);
		}
	}
}
