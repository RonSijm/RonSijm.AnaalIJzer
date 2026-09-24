using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public static class SolutionTopologyAnalysisService
{
    public static SolutionTopologyAnalysisResult Analyze(SolutionTopologyConfig config, IEnumerable<SolutionProjectReference> projectReferences)
    {
        var referenceViolations = ImmutableArray.CreateBuilder<SolutionTopologyReferenceViolation>();
        foreach (var reference in projectReferences
                     .OrderBy(reference => reference.SourceProjectName, StringComparer.Ordinal)
                     .ThenBy(reference => reference.TargetProjectName, StringComparer.Ordinal))
        {
            var evaluation = SolutionTopologyEvaluator.Evaluate(config, reference.SourceProjectName, reference.TargetProjectName);
            if (evaluation.IsAllowed)
            {
                continue;
            }

            referenceViolations.Add(new SolutionTopologyReferenceViolation(
                reference,
                evaluation.SourceModule,
                evaluation.TargetModule,
                evaluation.ViolationReason,
                evaluation.MatchedRule));
        }

        var configuredCycles = SolutionTopologyCycleDetector.FindConfiguredCycles(config);
        var result = new SolutionTopologyAnalysisResult(referenceViolations.ToImmutable(), configuredCycles);

        return result;
    }
}