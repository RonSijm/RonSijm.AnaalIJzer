using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Contracts.Contracts;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.EntryPoints;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.NameRules;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Config.Model;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.Policies;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.PolicyEvaluation;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;
using RonSijm.AnaalIJzer.Core.ReturnValues.Policies;
using RonSijm.AnaalIJzer.Core.SourceLocations;
using RonSijm.AnaalIJzer.Core.Visibility;

namespace RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

public readonly struct AnalyzerConfig(
	CompiledArchitectureConfig compiledConfig)
{
	private readonly ArchitecturePolicyEngine _engine = new(compiledConfig.LayerCatalog);

	public static readonly AnalyzerConfig Empty = new(CompiledArchitectureConfig.Empty);

	public static AnalyzerConfig Invalid(ConfigurationIssue issue)
	{
		var compiled = new CompiledArchitectureConfig(
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
			[issue]);
		var result = new AnalyzerConfig(compiled);

		return result;
	}

	public CompiledArchitectureConfig CompiledConfig { get; } = compiledConfig;
	public DependencyGraph Graph => CompiledConfig.Graph;
	public OutputConfig Output => CompiledConfig.Output;
	public ArchitectureDocumentation Documentation => CompiledConfig.Documentation;
	public ImmutableHashSet<string> RequiredRecognizedDependencySites => CompiledConfig.RequiredRecognizedDependencySites;
	public ImmutableDictionary<string, ImmutableHashSet<string>> LayerRequiredRecognizedDependencySites => CompiledConfig.LayerRequiredRecognizedDependencySites;
	public ArchitectureExceptionPolicy ExceptionPolicy => CompiledConfig.ExceptionPolicy;
	public ImmutableArray<ArchitectureExceptionDefinition> ExceptionDefinitions => CompiledConfig.ExceptionDefinitions;
	public ImmutableArray<ArchitectureExceptionReview> ExceptionReviews => CompiledConfig.ExceptionReviews;
	public bool EnforceAcyclic => CompiledConfig.EnforceAcyclic;
	public bool EnforceObservedAcyclic => CompiledConfig.EnforceObservedAcyclic;
	public ImmutableArray<ConfigurationIssue> ConfigurationIssues => CompiledConfig.ConfigurationIssues;
	public ImmutableArray<string> LayerNames => CompiledConfig.LayerNames;
	public ImmutableArray<LayerNode> Layers => CompiledConfig.LayerCatalog.Roots;
	public ImmutableArray<(string Name, string? Comment)> ForbiddenPatterns => CompiledConfig.ForbiddenPatterns;
	public ProjectArchitectureConfig ProjectArchitecture => CompiledConfig.ProjectArchitecture;

	public ImmutableHashSet<(string From, string To)> AllowedEdges => CompiledConfig.Graph.AllowedEdges;
	public ImmutableHashSet<string> WildcardTargets => CompiledConfig.Graph.WildcardTargets;
	public ImmutableHashSet<string> WildcardSources => CompiledConfig.Graph.WildcardSources;
	public bool AllowAnyDependency => CompiledConfig.Graph.AllowAnyDependency;
	public bool EnableReport => CompiledConfig.Output.EnableReport;
	public string ReportPath => CompiledConfig.Output.ReportPath;
	public bool EnableDocumentation => CompiledConfig.Output.EnableDocumentation;
	public string DocumentationPath => CompiledConfig.Output.DocumentationPath;

	public ArchitecturePolicyEngine Engine => _engine;

	public bool HasLayers => Engine.HasLayers;
	public bool HasExceptionReviews => !ExceptionReviews.IsDefaultOrEmpty;
	public bool HasProjectArchitecture => ProjectArchitecture.HasRules;
	public bool HasContractPolicies => Engine.HasContractPolicies;
	public bool HasInheritancePolicies => Engine.HasInheritancePolicies;
	public bool HasReturnValuePolicies => Engine.HasReturnValuePolicies;
	public bool HasVisibilityPolicies => Engine.HasVisibilityPolicies;
	public bool HasApiSurfacePolicies => Engine.HasApiSurfacePolicies;
	public bool HasEntryPointPolicies => Engine.HasEntryPointPolicies;
	public bool HasSourceLocationPolicies => Engine.HasSourceLocationPolicies;
	public bool HasConfigurationIssues => !ConfigurationIssues.IsDefaultOrEmpty;

	public bool RequiresRecognizedDependencyAt(string site)
	{
		var result = RequiredRecognizedDependencySites.Contains(site);

		return result;
	}

	public bool RequiresRecognizedDependencyAt(LayerMatch callerMatch, string site)
	{
		if (RequiresRecognizedDependencyAt(site))
		{
			return true;
		}

		foreach (var layer in callerMatch.Layers)
		{
			if (LayerRequiredRecognizedDependencySites.TryGetValue(layer.Name, out var sites) && sites.Contains(site))
			{
				return true;
			}
		}

		return false;
	}

	public LayerMatch? FindLayer(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = Engine.FindLayer(typeName, namespaceName, symbol);

		return result;
	}

	public TypePolicyViolation? EvaluateTypePolicy(LayerMatch layerMatch, string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = Engine.EvaluateTypePolicy(layerMatch, typeName, namespaceName, symbol);

		return result;
	}

	public NameRuleViolation? EvaluateNameRules(LayerMatch layerMatch, NameRuleTrigger trigger, NameRuleSubject source, NameRuleSubject target, string site)
	{
		var result = Engine.EvaluateNameRules(layerMatch, trigger, source, target, site);

		return result;
	}

	public ContractPolicyEvaluation? EvaluateContractPolicies(LayerMatch layerMatch, ContractDeclarationShape shape)
	{
		var result = Engine.EvaluateContractPolicies(layerMatch, shape);

		return result;
	}

	public InheritancePolicyEvaluation? EvaluateInheritancePolicies(LayerMatch layerMatch, INamedTypeSymbol symbol)
	{
		var result = Engine.EvaluateInheritancePolicies(layerMatch, symbol);

		return result;
	}

	public ReturnValuePolicyEvaluation? EvaluateReturnValuePolicies(LayerMatch layerMatch, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var result = Engine.EvaluateReturnValuePolicies(layerMatch, expression, semanticModel, cancellationToken);

		return result;
	}

	public VisibilityPolicyEvaluation? EvaluateVisibilityPolicies(LayerMatch layerMatch, VisibilityPolicyTarget target, ArchitectureAccessibility accessibility)
	{
		var result = Engine.EvaluateVisibilityPolicies(layerMatch, target, accessibility);

		return result;
	}

	public ApiSurfaceEvaluation? EvaluateApiSurfacePolicies(LayerMatch callerLayerMatch, LayerMatch? exposedLayerMatch, string exposedTypeName, string site, int exposureDepth = 0)
	{
		var result = Engine.EvaluateApiSurfacePolicies(callerLayerMatch, exposedLayerMatch, exposedTypeName, site, exposureDepth);

		return result;
	}

	public int GetTransitiveExposureMaxDepth(LayerMatch callerLayerMatch)
	{
		var result = Engine.GetTransitiveExposureMaxDepth(callerLayerMatch);

		return result;
	}

	public BoundaryEntryPointEvaluation EvaluateBoundaryEntryPoints(LayerMatch callerMatch, LayerMatch dependencyMatch, string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType, string site)
	{
		var result = Engine.EvaluateBoundaryEntryPoints(callerMatch, dependencyMatch, dependencyTypeName, dependencyNamespace, dependencyType, site);

		return result;
	}

	public ImmutableArray<SourceLocationPolicy> GetSourceLocationPolicies(LayerMatch layerMatch)
	{
		var result = Engine.GetSourceLocationPolicies(layerMatch);

		return result;
	}
}
