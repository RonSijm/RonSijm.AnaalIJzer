using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.GraphModel.Model;
using RonSijm.AnaalIJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
	private static void AddTypeEvidence(ProjectAnalysisResult project, AnalyzerConfiguration config, ImmutableArray<ArchitectureGraphTypeEvidence>.Builder types, HashSet<string> seenTypes, CancellationToken cancellationToken)
	{
		foreach (var type in CompilationTypeCollector.GetProjectTypes(project.Compilation, cancellationToken))
		{
			var match = FindLayer(config, type);
			if (match is null || match.Value.Layer.IsForbidden)
			{
				continue;
			}

			var location = type.Locations.FirstOrDefault(location => location.IsInSource);
			var filePath = GetLocationPath(location);
			var lineNumber = GetLineNumber(location);
			var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
			var key = match.Value.Layer.Name + "|" + fullTypeName + "|" + filePath + "|" + lineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if (!seenTypes.Add(key))
			{
				continue;
			}

			types.Add(new ArchitectureGraphTypeEvidence(
				match.Value.Layer.Name,
				type.Name,
				fullTypeName,
				filePath,
				lineNumber));
		}
	}

	private static LayerMatch? FindLayer(AnalyzerConfiguration config, INamedTypeSymbol type)
	{
		var result = config.Engine.FindLayer(type.Name, GetNamespace(type), type);

		return result;
	}

	private static string GetNamespace(INamedTypeSymbol type)
	{
		var result = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();

		return result;
	}

	private static string GetLocationPath(Location? location)
	{
		var result = location.GetSourcePath();

		return result;
	}

	private static int GetLineNumber(Location? location)
	{
		var result = location.GetSourceLineNumber();

		return result;
	}
}

