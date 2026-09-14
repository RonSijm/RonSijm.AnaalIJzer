using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Symbols;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;
using RonSijm.AnaalIJzer.Core.Statistics.Aggregation;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Core.Visibility;

namespace RonSijm.AnaalIJzer.Statistics.Roslyn.Analysis;

public static class CompilationStatisticsCollector
{
	public static StatisticsProjectSnapshot Collect(Compilation compilation, StatisticsProjectIdentity identity, CancellationToken cancellationToken)
	{
		var result = Collect(compilation, identity, GeneratedCodeAnalysisScope.Exclude, cancellationToken);

		return result;
	}

	public static StatisticsProjectSnapshot Collect(Compilation compilation, StatisticsProjectIdentity identity, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
	{
		var counter = new StatisticsMeasurementCounter();
		var types = CompilationTypeCollector.GetProjectTypes(compilation, generatedCodeScope, cancellationToken);
		foreach (var type in types)
		{
			var typeKind = GetTypeKind(type);
			var accessibility = GetAccessibility(type);
			counter.Record(StatisticsDimension.TypeKind, typeKind);
			counter.Record(StatisticsDimension.TypeAccessibility, accessibility);
			counter.RecordGrouped(StatisticsDimension.TypeKind, typeKind, StatisticsDimension.TypeAccessibility, accessibility);
			counter.RecordGrouped(StatisticsDimension.TypeAccessibility, accessibility, StatisticsDimension.TypeKind, typeKind);
		}

		var members = GetProjectMembers(types, generatedCodeScope, cancellationToken).ToArray();
		foreach (var member in members)
		{
			var memberKind = GetMemberKind(member);
			var accessibility = GetAccessibility(member);
			counter.Record(StatisticsDimension.MemberKind, memberKind);
			counter.Record(StatisticsDimension.MemberAccessibility, accessibility);
			counter.RecordGrouped(StatisticsDimension.MemberKind, memberKind, StatisticsDimension.MemberAccessibility, accessibility);
			counter.RecordGrouped(StatisticsDimension.MemberAccessibility, accessibility, StatisticsDimension.MemberKind, memberKind);
		}

		foreach (var observation in DependencySiteObservationScanner.Scan(compilation, generatedCodeScope, cancellationToken))
		{
			counter.Record(StatisticsDimension.DependencySite, observation.Site);
			counter.RecordGrouped(StatisticsDimension.DependencySite, observation.Site, StatisticsDimension.TypeKind, GetTypeKind(observation.CallerType));
			counter.RecordGrouped(StatisticsDimension.DependencySite, observation.Site, StatisticsDimension.TypeAccessibility, GetAccessibility(observation.CallerType));
		}

		var sourceFileCount = compilation.SyntaxTrees.Count(tree => generatedCodeScope.ShouldAnalyze(tree, cancellationToken));
		var compilerErrorCount = compilation.GetDiagnostics(cancellationToken).Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		var result = new StatisticsProjectSnapshot(identity, counter.GetMeasurements(), sourceFileCount, types.Count, members.Length, compilerErrorCount, compilerErrorCount, counter.GetGroupedMeasurements());

		return result;
	}

	private static IEnumerable<ISymbol> GetProjectMembers(IReadOnlyList<INamedTypeSymbol> types, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
	{
		var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var type in types)
		{
			foreach (var member in type.GetMembers())
			{
				if (!ShouldCountMember(member, generatedCodeScope, cancellationToken) || !seen.Add(member))
				{
					continue;
				}

				yield return member;
			}
		}
	}

	private static bool ShouldCountMember(ISymbol member, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
	{
		if (member is INamedTypeSymbol || member.IsImplicitlyDeclared || member is IMethodSymbol { AssociatedSymbol: not null })
		{
			return false;
		}

		var result = member.Locations.Any(location => location.IsInSource && location.SourceTree is not null && generatedCodeScope.ShouldAnalyze(location.SourceTree, cancellationToken));

		return result;
	}

	private static string GetTypeKind(INamedTypeSymbol type)
	{
		foreach (var typeKind in StatisticsDimensionCatalog.TypeKinds)
		{
			if (typeKind != StatisticsDimensionCatalog.Other && type.HasTypeKind(typeKind))
			{
				return typeKind;
			}
		}

		var result = StatisticsDimensionCatalog.Other;

		return result;
	}

	private static string GetMemberKind(ISymbol member)
	{
		var result = member.TryGetSemanticOperationMemberKind(out var memberKind)
			? memberKind.ToString()
			: StatisticsDimensionCatalog.Other;

		return result;
	}

	private static string GetAccessibility(ISymbol symbol)
	{
		var result = symbol.TryGetArchitectureAccessibility(out var accessibility)
			? accessibility.ToDisplayText()
			: StatisticsDimensionCatalog.NotApplicable;

		return result;
	}
}
