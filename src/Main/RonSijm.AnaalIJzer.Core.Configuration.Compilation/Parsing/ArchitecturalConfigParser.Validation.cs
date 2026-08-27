using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Compilation;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Config.Model;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.Policies;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static bool TryContinueAfterDocumentIntake(
		ArchitectureConfigurationDocumentParseContext documentContext,
		ImmutableArray<ConfigurationIssue>.Builder issues,
		out AnalyzerConfig earlyResult)
	{
		_ = issues;

		if (documentContext.Documents.IsDefaultOrEmpty)
		{
			earlyResult = AnalyzerConfig.Empty;
			return false;
		}

		earlyResult = AnalyzerConfig.Empty;
		return true;
	}

	private static AnalyzerConfig CreateInvalidConfig(ImmutableArray<ConfigurationIssue> issues)
	{
		var config = AnalyzerConfig.Invalid(issues[0]);
		if (issues.Length == 1)
		{
			return config;
		}

		var compiledConfig = new CompiledArchitectureConfig(
			CompiledLayerCatalog.Empty,
			new DependencyGraph(ImmutableArray<DependencyEdge>.Empty),
			new OutputConfig(false, string.Empty, false, string.Empty),
			ImmutableHashSet<string>.Empty,
			ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
			ArchitectureExceptionPolicy.Disabled,
			ImmutableArray<ArchitectureExceptionDefinition>.Empty,
			ImmutableArray<ArchitectureExceptionReview>.Empty,
			false,
			false,
			ImmutableArray<string>.Empty,
			ImmutableArray<(string, string?)>.Empty,
			ProjectArchitectureConfig.Empty,
			ArchitectureDocumentation.Empty,
			issues);
		var result = new AnalyzerConfig(compiledConfig);

		return result;
	}

	private static void AddIssue(ImmutableArray<ConfigurationIssue>.Builder issues, ConfigurationIssueKind kind, string message, XElement element, string path)
	{
		var line = (IXmlLineInfo)element;
		issues.Add(new ConfigurationIssue(kind, message, path, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
	}
}

