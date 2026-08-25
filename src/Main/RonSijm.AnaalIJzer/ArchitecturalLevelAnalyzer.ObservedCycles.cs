using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.ObservedDependencies;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

public sealed partial class ArchitecturalLevelAnalyzer
{
	private static void ReportObservedDependencyCycles(CompilationAnalysisContext context, AnalyzerConfig config, ObservedDependencyCollector collector)
	{
		foreach (var cycle in ObservedDependencyCycleEvaluator.FindCycles(config.LayerNames, collector.GetSnapshot(), "Project"))
		{
			var primaryEdge = cycle.RepresentativeEdges[0];
			var additionalLocations = cycle.RepresentativeEdges
				.Skip(1)
				.Select(edge => edge.Location)
				.Where(location => location != Location.None)
				.ToImmutableArray();
			var properties = ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertyCycleLayers, string.Join("|", cycle.Layers))
				.Add(ArchitecturalDiagnostics.PropertyCycleLength, cycle.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyObservedSites, string.Join(", ", cycle.ObservedSites))
				.Add(ArchitecturalDiagnostics.PropertyCycleScope, cycle.Scope)
				.Add(ArchitecturalDiagnostics.PropertySourceProjectName, context.Compilation.AssemblyName);
			context.ReportDiagnostic(Diagnostic.Create(
				ArchitecturalDiagnostics.ObservedDependencyCycle,
				primaryEdge.Location,
				additionalLocations,
				properties,
				cycle.GetDisplayPath()));
		}
	}
}
