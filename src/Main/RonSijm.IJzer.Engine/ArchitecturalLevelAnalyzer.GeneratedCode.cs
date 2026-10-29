using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.IJzer.Engine.Analysis.GeneratedCode;
using AnalyzerConfig = RonSijm.IJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.IJzer.Engine;

public sealed partial class ArchitecturalLevelAnalyzer
{
    private static void AnalyzeSyntaxNodeWhenInScope(SyntaxNodeAnalysisContext context, AnalyzerConfig config, Action analyze)
    {
        if (!GeneratedCodeAnalysisGate.ShouldAnalyze(context.Node.SyntaxTree, config, context.CancellationToken))
        {
            return;
        }

        analyze();
    }
}