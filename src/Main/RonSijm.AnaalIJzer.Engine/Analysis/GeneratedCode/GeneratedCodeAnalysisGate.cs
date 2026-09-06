using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Observations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.GeneratedCode;

internal static class GeneratedCodeAnalysisGate
{
	internal static bool ShouldAnalyze(SyntaxTree syntaxTree, AnalyzerConfig config, CancellationToken cancellationToken)
	{
		var result = config.GeneratedCodeScope.ShouldAnalyze(syntaxTree, cancellationToken);

		return result;
	}

	internal static bool ShouldAnalyze(SymbolAnalysisContext context, AnalyzerConfig config)
	{
		foreach (var syntaxReference in context.Symbol.DeclaringSyntaxReferences)
		{
			if (ShouldAnalyze(syntaxReference.SyntaxTree, config, context.CancellationToken))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool ShouldAnalyze(OperationAnalysisContext context, AnalyzerConfig config)
	{
		var result = ShouldAnalyze(context.Operation.Syntax.SyntaxTree, config, context.CancellationToken);

		return result;
	}

	internal static bool ShouldAnalyze(OperationBlockAnalysisContext context, AnalyzerConfig config)
	{
		foreach (var operationBlock in context.OperationBlocks)
		{
			if (ShouldAnalyze(operationBlock.Syntax.SyntaxTree, config, context.CancellationToken))
			{
				return true;
			}
		}

		return false;
	}
}
