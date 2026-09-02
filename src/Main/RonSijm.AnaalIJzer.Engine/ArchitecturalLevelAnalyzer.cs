using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;
using RonSijm.AnaalIJzer.Engine.Analysis.Placement.SourceLocations;
using RonSijm.AnaalIJzer.Engine.Analysis.Topology.ProjectArchitecture;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ApiSurface;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Contracts;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Inheritance;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ReturnValues;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Visibility;

namespace RonSijm.AnaalIJzer.Engine;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ArchitecturalLevelAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		ArchitecturalDiagnostics.IllegalDependency,
		ArchitecturalDiagnostics.UnrecognizedDependency,
		ArchitecturalDiagnostics.ForbiddenDependency,
		ArchitecturalDiagnostics.WrongDirectionDependency,
		ArchitecturalDiagnostics.SameLayerDependency,
		ArchitecturalDiagnostics.InvalidConfiguration,
		ArchitecturalDiagnostics.CyclicDependencyGraph,
		ArchitecturalDiagnostics.NameRuleViolation,
		ArchitecturalDiagnostics.ApiSurfaceLeakage,
		ArchitecturalDiagnostics.ProjectReferenceViolation,
		ArchitecturalDiagnostics.PackageReferenceViolation,
		ArchitecturalDiagnostics.VisibilityPolicyViolation,
		ArchitecturalDiagnostics.ContractPurityViolation,
		ArchitecturalDiagnostics.InheritancePolicyViolation,
		ArchitecturalDiagnostics.ReturnValuePolicyViolation,
		ArchitecturalDiagnostics.ForbiddenTransitiveExposure,
		ArchitecturalDiagnostics.SourceLocationViolation,
		ArchitecturalDiagnostics.BoundaryEntryPointViolation,
		ArchitecturalDiagnostics.ExceptionReview,
		ArchitecturalDiagnostics.ObservedDependencyCycle,
	];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationContext =>
		{
			var config = ArchitecturalConfigParser.Parse(compilationContext.Options.AdditionalFiles, compilationContext.Compilation, compilationContext.CancellationToken);
			if (config.HasConfigurationIssues)
			{
				compilationContext.RegisterCompilationEndAction(reportContext => ReportConfigurationIssues(reportContext, config, compilationContext.Options.AdditionalFiles));
			}

			if (config.HasExceptionReviews)
			{
				compilationContext.RegisterCompilationEndAction(reportContext => ReportExceptionReviews(reportContext, config, compilationContext.Options.AdditionalFiles));
			}

			if (config.HasProjectArchitecture)
			{
				compilationContext.RegisterCompilationEndAction(reportContext => ProjectReferenceAnalyzer.AnalyzeCompilation(reportContext, config, compilationContext.Options.AdditionalFiles));
			}

			if (!config.Engine.HasLayers)
			{
				return;
			}

			var violations = new ConcurrentBag<ViolationRecord>();
			var observedDependencies = config.EnforceObservedAcyclic ? new ObservedDependencyCollector() : null;
			var buildProperties = AnalyzerBuildProperties.Read(compilationContext.Options.AnalyzerConfigOptionsProvider);

			if (config.Engine.HasVisibilityPolicies)
			{
				var analyzedVisibilitySymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
				compilationContext.RegisterSymbolAction(symbolContext => VisibilityPolicyAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedVisibilitySymbols), SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);
			}

			if (config.Engine.HasContractPolicies)
			{
				var analyzedContractSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
				compilationContext.RegisterSymbolAction(symbolContext => ContractPurityAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedContractSymbols), SymbolKind.NamedType);
			}

			if (config.Engine.HasInheritancePolicies)
			{
				var analyzedInheritanceSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
				compilationContext.RegisterSymbolAction(symbolContext => InheritancePolicyAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedInheritanceSymbols), SymbolKind.NamedType);
			}

			if (config.Engine.HasReturnValuePolicies)
			{
				compilationContext.RegisterSyntaxNodeAction(nodeContext => ReturnValuePolicyAnalyzer.AnalyzeReturnStatement(nodeContext, config), SyntaxKind.ReturnStatement);
				compilationContext.RegisterSyntaxNodeAction(nodeContext => ReturnValuePolicyAnalyzer.AnalyzeArrowExpressionClause(nodeContext, config), SyntaxKind.ArrowExpressionClause);
			}

			if (config.Engine.HasApiSurfacePolicies)
			{
				var analyzedApiSurfaceSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
				var transitiveMemberCache = new ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>>(SymbolEqualityComparer.Default);
				compilationContext.RegisterSymbolAction(symbolContext => ApiSurfaceAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedApiSurfaceSymbols, transitiveMemberCache), SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);
			}

			if (config.Engine.HasSourceLocationPolicies)
			{
				var analyzedSourceLocationSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
				compilationContext.RegisterSymbolAction(symbolContext => LayerSourceLocationAnalyzer.AnalyzeSymbol(symbolContext, config, buildProperties, analyzedSourceLocationSymbols), SymbolKind.NamedType);
			}

			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeConstructorDeclaration(nodeContext, config, violations, observedDependencies), SyntaxKind.ConstructorDeclaration);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeTypeDeclaration(nodeContext, config, violations, observedDependencies), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeMethodDeclaration(nodeContext, config, violations, observedDependencies), SyntaxKind.MethodDeclaration);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeFieldDeclaration(nodeContext, config, violations, observedDependencies), SyntaxKind.FieldDeclaration);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzePropertyDeclaration(nodeContext, config, violations, observedDependencies), SyntaxKind.PropertyDeclaration);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeObjectCreation(nodeContext, config, violations, observedDependencies), SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeInvocation(nodeContext, config, violations, observedDependencies), SyntaxKind.InvocationExpression);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeLocalDeclaration(nodeContext, config, violations, observedDependencies), SyntaxKind.LocalDeclarationStatement);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeAttribute(nodeContext, config, violations, observedDependencies), SyntaxKind.Attribute);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => LayerDependencyAnalyzer.AnalyzeStaticMemberAccess(nodeContext, config, violations, observedDependencies), SyntaxKind.SimpleMemberAccessExpression);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeAssignmentExpression(nodeContext, config, violations), SyntaxKind.SimpleAssignmentExpression);
			compilationContext.RegisterSyntaxNodeAction(nodeContext => Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeReturnStatement(nodeContext, config, violations), SyntaxKind.ReturnStatement);
			if (observedDependencies is not null)
			{
				compilationContext.RegisterCompilationEndAction(reportContext => ReportObservedDependencyCycles(reportContext, config, observedDependencies));
			}
		});
	}
}
