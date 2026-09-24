using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Engine.Analysis.AssemblyAttributes;
using RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;
using RonSijm.AnaalIJzer.Engine.Analysis.GeneratedCode;
using RonSijm.AnaalIJzer.Engine.Analysis.Operations;
using RonSijm.AnaalIJzer.Engine.Analysis.Placement.SourceLocations;
using RonSijm.AnaalIJzer.Engine.Analysis.Topology.ProjectArchitecture;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ApiSurface;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Contracts;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Inheritance;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Operations;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ReturnValues;
using RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Visibility;

namespace RonSijm.AnaalIJzer.Engine;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ArchitecturalLevelAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ArchitecturalDiagnostics.DependencyNotAllowed,
        ArchitecturalDiagnostics.DependencyRequiredMissing,
        ArchitecturalDiagnostics.TypeNotAllowed,
        ArchitecturalDiagnostics.DependencyReverseDirection,
        ArchitecturalDiagnostics.DependencyPeerScope,
        ArchitecturalDiagnostics.ConfigurationInvalid,
        ArchitecturalDiagnostics.ConfigurationCycle,
        ArchitecturalDiagnostics.NameShapeMismatch,
        ArchitecturalDiagnostics.ApiExposureNotAllowed,
        ArchitecturalDiagnostics.ProjectReferenceNotAllowed,
        ArchitecturalDiagnostics.PackageReferenceNotAllowed,
        ArchitecturalDiagnostics.VisibilityNotAllowed,
        ArchitecturalDiagnostics.ContractShapeMismatch,
        ArchitecturalDiagnostics.InheritanceNotAllowed,
        ArchitecturalDiagnostics.ReturnNotAllowed,
        ArchitecturalDiagnostics.OperationNotAllowed,
        ArchitecturalDiagnostics.OperationRequiredMissing,
        ArchitecturalDiagnostics.OperationCardinality,
        ArchitecturalDiagnostics.OperationOrdering,
        ArchitecturalDiagnostics.OperationContractNotAllowed,
        ArchitecturalDiagnostics.OperationContractRequiredMissing,
        ArchitecturalDiagnostics.OperationContractShapeMismatch,
        ArchitecturalDiagnostics.AssemblyAttributeNotAllowed,
        ArchitecturalDiagnostics.NamespaceBoundaryPlacement,
        ArchitecturalDiagnostics.ApiTransitiveExposure,
        ArchitecturalDiagnostics.SourceBoundaryPlacement,
        ArchitecturalDiagnostics.BoundaryEntryPlacement,
        ArchitecturalDiagnostics.ExceptionReviewLifecycle,
        ArchitecturalDiagnostics.DependencyCycle,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
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

            if (config.HasOperationContracts)
            {
                compilationContext.RegisterCompilationEndAction(reportContext => OperationContractAnalyzer.AnalyzeCompilation(reportContext, config));
            }

            if (config.HasAssemblyAttributePolicies)
            {
                compilationContext.RegisterCompilationEndAction(reportContext => AssemblyAttributePolicyAnalyzer.AnalyzeCompilation(reportContext, config));
            }

            if (config.Engine.HasReturnValuePolicies)
            {
                compilationContext.RegisterSyntaxNodeAction(nodeContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(nodeContext.Node.SyntaxTree, config, nodeContext.CancellationToken))
                    {
                        ReturnValuePolicyAnalyzer.AnalyzeReturnStatement(nodeContext, config);
                    }
                }, SyntaxKind.ReturnStatement);
                compilationContext.RegisterSyntaxNodeAction(nodeContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(nodeContext.Node.SyntaxTree, config, nodeContext.CancellationToken))
                    {
                        ReturnValuePolicyAnalyzer.AnalyzeArrowExpressionClause(nodeContext, config);
                    }
                }, SyntaxKind.ArrowExpressionClause);
            }

            if (!config.Engine.HasLayers && !config.HasNamespaceHierarchyPolicies)
            {
                return;
            }

            var violations = new ConcurrentBag<ViolationRecord>();
            var observedDependencies = config.EnforceObservedAcyclic ? new ObservedDependencyCollector() : null;
            var buildProperties = AnalyzerBuildProperties.Read(compilationContext.Options.AnalyzerConfigOptionsProvider);

            if (config.Engine.HasVisibilityPolicies)
            {
                var analyzedVisibilitySymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(symbolContext, config))
                    {
                        VisibilityPolicyAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedVisibilitySymbols);
                    }
                }, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);
            }

            if (config.Engine.HasContractPolicies)
            {
                var analyzedContractSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(symbolContext, config))
                    {
                        ContractPurityAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedContractSymbols);
                    }
                }, SymbolKind.NamedType);
            }

            if (config.Engine.HasInheritancePolicies)
            {
                var analyzedInheritanceSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(symbolContext, config))
                    {
                        InheritancePolicyAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedInheritanceSymbols);
                    }
                }, SymbolKind.NamedType);
            }

            if (config.Engine.HasForbiddenOperationPolicies)
            {
                compilationContext.RegisterOperationAction(operationContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(operationContext, config))
                    {
                        ForbiddenOperationPolicyAnalyzer.AnalyzeOperation(operationContext, config);
                    }
                },
                    OperationKind.Invocation,
                    OperationKind.PropertyReference,
                    OperationKind.FieldReference,
                    OperationKind.EventReference,
                    OperationKind.ObjectCreation,
                    OperationKind.Conversion,
                    OperationKind.SimpleAssignment,
                    OperationKind.CompoundAssignment,
                    OperationKind.Return,
                    OperationKind.Argument);
            }

            if (config.Engine.HasBehavioralOperationPolicies)
            {
                compilationContext.RegisterOperationBlockAction(operationBlockContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(operationBlockContext, config))
                    {
                        BehavioralOperationPolicyAnalyzer.AnalyzeOperationBlock(operationBlockContext, config);
                    }
                });
            }

            if (config.Engine.HasApiSurfacePolicies)
            {
                var analyzedApiSurfaceSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
                var transitiveMemberCache = new ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>>(SymbolEqualityComparer.Default);
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(symbolContext, config))
                    {
                        ApiSurfaceAnalyzer.AnalyzeSymbol(symbolContext, config, analyzedApiSurfaceSymbols, transitiveMemberCache);
                    }
                }, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);
            }

            if (config.Engine.HasSourceLocationPolicies)
            {
                var analyzedSourceLocationSymbols = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(symbolContext, config))
                    {
                        LayerSourceLocationAnalyzer.AnalyzeSymbol(symbolContext, config, buildProperties, analyzedSourceLocationSymbols);
                    }
                }, SymbolKind.NamedType);
            }

            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeConstructorDeclaration(nodeContext, config, violations, observedDependencies)), SyntaxKind.ConstructorDeclaration);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeTypeDeclaration(nodeContext, config, violations, observedDependencies)), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeMethodDeclaration(nodeContext, config, violations, observedDependencies)), SyntaxKind.MethodDeclaration);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeFieldDeclaration(nodeContext, config, violations, observedDependencies)), SyntaxKind.FieldDeclaration);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzePropertyDeclaration(nodeContext, config, violations, observedDependencies)), SyntaxKind.PropertyDeclaration);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeObjectCreation(nodeContext, config, violations, observedDependencies)), SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeInvocation(nodeContext, config, violations, observedDependencies)), SyntaxKind.InvocationExpression);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeLocalDeclaration(nodeContext, config, violations, observedDependencies)), SyntaxKind.LocalDeclarationStatement);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeAttribute(nodeContext, config, violations, observedDependencies)), SyntaxKind.Attribute);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => LayerDependencyAnalyzer.AnalyzeStaticMemberAccess(nodeContext, config, violations, observedDependencies)), SyntaxKind.SimpleMemberAccessExpression);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeAssignmentExpression(nodeContext, config, violations)),
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.CoalesceAssignmentExpression,
                SyntaxKind.UnsignedRightShiftAssignmentExpression);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeReturnStatement(nodeContext, config, violations)), SyntaxKind.ReturnStatement);
            compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeArrowExpressionNameRules(nodeContext, config, violations)), SyntaxKind.ArrowExpressionClause);
            if (config.HasIntraProceduralNameRules)
            {
                compilationContext.RegisterOperationBlockAction(operationBlockContext =>
                {
                    if (GeneratedCodeAnalysisGate.ShouldAnalyze(operationBlockContext, config))
                    {
                        Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeIntraProceduralNameRules(operationBlockContext, config, violations);
                    }
                });
                compilationContext.RegisterSyntaxNodeAction(nodeContext => AnalyzeSyntaxNodeWhenInScope(nodeContext, config, () => Analysis.NamingRules.LayerDependencyAnalyzer.AnalyzeIntraProceduralLambdaNameRules(nodeContext, config, violations)), SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression);
            }
            if (observedDependencies is not null)
            {
                compilationContext.RegisterCompilationEndAction(reportContext => ReportObservedDependencyCycles(reportContext, config, observedDependencies));
            }
        });
    }
}