using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
	private static string FormatRuleLocation(ArchitectureDocumentationItem item)
	{
		var result = item.XmlLineNumber > 0 ? $"{item.SourcePath}:{item.XmlLineNumber}" : item.SourcePath;

		return result;
	}

	private static string FormatDiagnosticLocation(Diagnostic diagnostic, string projectDirectory)
	{
		if (!diagnostic.Location.IsInSource)
		{
			return string.Empty;
		}

		var lineSpan = diagnostic.Location.GetLineSpan();
		var path = string.IsNullOrWhiteSpace(lineSpan.Path) ? string.Empty : Path.GetRelativePath(projectDirectory, lineSpan.Path);
		var result = $"{path}:{lineSpan.StartLinePosition.Line + 1}";

		return result;
	}

	private static string FormatObservedEdgeLocation(ObservedDependency edge, IReadOnlyList<ProjectAnalysisResult> projects)
	{
		if (!edge.Location.IsInSource)
		{
			return string.Empty;
		}

		var lineSpan = edge.Location.GetLineSpan();
		var path = lineSpan.Path ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(edge.SourceProjectName))
		{
			var project = projects.FirstOrDefault(candidate => string.Equals(candidate.AssemblyName, edge.SourceProjectName, StringComparison.Ordinal));
			if (project is not null && !string.IsNullOrWhiteSpace(path))
			{
				path = Path.GetRelativePath(project.ProjectDirectory, path);
			}
		}

		var result = $"{edge.SourceProjectName ?? string.Empty}:{path}:{lineSpan.StartLinePosition.Line + 1}".Trim(':');

		return result;
	}
}

