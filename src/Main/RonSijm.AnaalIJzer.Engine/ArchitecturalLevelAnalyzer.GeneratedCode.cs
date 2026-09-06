using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Engine.Analysis.GeneratedCode;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine;

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
