using System.Collections.Immutable;
using RonSijm.IJzer.GraphModel.Model;
using RonSijm.IJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.IJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.IJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
    private static ArchitectureGraphEvidence CreateEvidence(ImmutableArray<ProjectAnalysisResult> projects, AnalyzerConfiguration config, CancellationToken cancellationToken)
    {
        var types = ImmutableArray.CreateBuilder<ArchitectureGraphTypeEvidence>();
        var dependencies = ImmutableArray.CreateBuilder<ArchitectureGraphDependencyEvidence>();
        var seenTypes = new HashSet<string>(StringComparer.Ordinal);
        var seenDependencies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var project in projects)
        {
            AddTypeEvidence(project, config, types, seenTypes, cancellationToken);
            AddDependencyEvidence(project, config, dependencies, seenDependencies, cancellationToken);
        }

        var result = new ArchitectureGraphEvidence(types.ToImmutable(), dependencies.ToImmutable());

        return result;
    }
}