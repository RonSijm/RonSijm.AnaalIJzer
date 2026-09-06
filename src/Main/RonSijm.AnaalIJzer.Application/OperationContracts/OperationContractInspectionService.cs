using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.OperationContracts.Evaluation;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;
using RonSijm.AnaalIJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application.OperationContracts;

/// <summary>Checks solution-wide owner cardinality for explicitly configured operation contracts.</summary>
internal static class OperationContractInspectionService
{
	internal static IReadOnlyList<ArchitectureFinding> GetFindings(IReadOnlyList<ProjectAnalysisResult> projects, CancellationToken cancellationToken)
	{
		var findings = new List<ArchitectureFinding>();
		foreach (var group in GroupConfiguredProjects(projects))
		{
			var config = group[0].Config;
			var candidates = GetMethods(projects, config, cancellationToken);
			foreach (var definition in config.OperationContracts.Definitions)
			{
				var owners = candidates
					.Where(candidate => OperationContractEvaluator.MatchesOwner(definition, candidate.Method))
					.OrderBy(candidate => candidate.ProjectName, StringComparer.Ordinal)
					.ThenBy(candidate => candidate.DisplayName, StringComparer.Ordinal)
					.ToArray();
				if (owners.Length == 1)
				{
					continue;
				}

				findings.Add(CreateOwnerFinding(definition, owners));
			}
		}

		return findings;
	}

	private static IEnumerable<IReadOnlyList<ProjectAnalysisResult>> GroupConfiguredProjects(IReadOnlyList<ProjectAnalysisResult> projects)
	{
		var groups = projects
			.Where(project => project.Config.HasOperationContracts)
			.GroupBy(GetConfigurationKey, StringComparer.OrdinalIgnoreCase)
			.Select(group => (IReadOnlyList<ProjectAnalysisResult>)group.ToArray());

		return groups;
	}

	private static string GetConfigurationKey(ProjectAnalysisResult project)
	{
		if (!string.IsNullOrWhiteSpace(project.ConfigInputPath))
		{
			var result = "file:" + Path.GetFullPath(project.ConfigInputPath);

			return result;
		}

		if (!string.IsNullOrWhiteSpace(project.InlineConfigSourcePath))
		{
			var result = "inline:" + Path.GetFullPath(project.InlineConfigSourcePath);

			return result;
		}

		var fallback = "project:" + Path.GetFullPath(project.ProjectPath);

		return fallback;
	}

	private static ImmutableArray<OperationMethodCandidate> GetMethods(IReadOnlyList<ProjectAnalysisResult> projects, AnalyzerConfiguration config, CancellationToken cancellationToken)
	{
		var candidates = ImmutableArray.CreateBuilder<OperationMethodCandidate>();
		foreach (var project in projects)
		{
			var projectName = project.AssemblyName ?? Path.GetFileNameWithoutExtension(project.ProjectPath);
			foreach (var type in CompilationTypeCollector.GetProjectTypes(project.Compilation, config.GeneratedCodeScope, cancellationToken))
			{
				foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
				{
					if (method.MethodKind != MethodKind.Ordinary || !HasAnalyzableSource(method, config.GeneratedCodeScope, cancellationToken))
					{
						continue;
					}

					candidates.Add(new OperationMethodCandidate(projectName, method));
				}
			}
		}

		var result = candidates.ToImmutable();

		return result;
	}

	private static bool HasAnalyzableSource(IMethodSymbol method, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
	{
		foreach (var syntaxReference in method.DeclaringSyntaxReferences)
		{
			if (generatedCodeScope.ShouldAnalyze(syntaxReference.SyntaxTree, cancellationToken))
			{
				return true;
			}
		}

		return false;
	}

	private static ArchitectureFinding CreateOwnerFinding(OperationContractDefinition definition, IReadOnlyList<OperationMethodCandidate> owners)
	{
		var location = FormatLocation(definition.XmlPath, definition.XmlLineNumber);
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertyOperationContractName, definition.Name)
			.Add(ArchitectureDiagnosticProperties.PropertyRuleXmlPath, definition.XmlPath)
			.Add(ArchitectureDiagnosticProperties.PropertyRuleXmlLine, definition.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
		if (owners.Count == 0)
		{
			var result = new ArchitectureFinding(
				ArchitectureFindingSeverity.Error,
				ArchitectureFindingCodes.OperationContractOwnerMissing,
				"Operation '" + definition.Name + "' has no configured owner method in the inspected scope.",
				location,
				"MissingOwner",
				"MissingOwner",
				properties);

			return result;
		}

		var candidates = string.Join("; ", owners.Select(owner => owner.ProjectName + ": " + owner.DisplayName));
		var ambiguousResult = new ArchitectureFinding(
			ArchitectureFindingSeverity.Error,
			ArchitectureFindingCodes.OperationContractOwnerAmbiguous,
			"Operation '" + definition.Name + "' matches " + owners.Count + " owner methods; exactly one owner is required.",
			location + " - " + candidates,
			"AmbiguousOwner",
			"AmbiguousOwner",
			properties);

		return ambiguousResult;
	}

	private static string FormatLocation(string path, int lineNumber)
	{
		var result = lineNumber > 0 ? path + ":" + lineNumber : path;

		return result;
	}

	private sealed class OperationMethodCandidate(string projectName, IMethodSymbol method)
	{
		public string ProjectName { get; } = projectName;

		public IMethodSymbol Method { get; } = method;

		public string DisplayName
		{
			get
			{
				var result = Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

				return result;
			}
		}
	}
}
