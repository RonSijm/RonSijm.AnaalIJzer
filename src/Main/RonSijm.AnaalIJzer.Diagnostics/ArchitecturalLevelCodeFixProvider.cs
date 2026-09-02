using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Diagnostics;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArchitecturalLevelCodeFixProvider))]
[Shared]
public sealed class ArchitecturalLevelCodeFixProvider : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds =>
	[
		ArchitecturalDiagnosticIds.IllegalLevelDependency,
		ArchitecturalDiagnosticIds.UnrecognizedDependency,
		ArchitecturalDiagnosticIds.NameRuleViolation,
		ArchitecturalDiagnosticIds.ForbiddenDependency,
		ArchitecturalDiagnosticIds.WrongDirectionDependency,
		ArchitecturalDiagnosticIds.SameLayerDependency,
		ArchitecturalDiagnosticIds.ApiSurfaceLeakage,
		ArchitecturalDiagnosticIds.ProjectReferenceViolation,
		ArchitecturalDiagnosticIds.PackageReferenceViolation,
		ArchitecturalDiagnosticIds.VisibilityPolicyViolation,
		ArchitecturalDiagnosticIds.ContractPurityViolation,
		ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure,
		ArchitecturalDiagnosticIds.InheritancePolicyViolation,
		ArchitecturalDiagnosticIds.SourceLocationViolation,
		ArchitecturalDiagnosticIds.BoundaryEntryPointViolation,
		ArchitecturalDiagnosticIds.CyclicDependencyGraph,
	];

	// Rename is not batch-safe (each rename changes all references), so no FixAll.
	public override FixAllProvider? GetFixAllProvider()
	{
		FixAllProvider? result = null;

		return result;
	}

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		foreach (var diagnostic in context.Diagnostics)
		{
			if (diagnostic.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			{
				await RecognizedDependencyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation)
			{
				await DeclarationNameCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
				await NameRuleAllowMappingCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

            if (diagnostic.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
            {
                await TryRegisterRenameAsync(context, diagnostic).ConfigureAwait(false);
				await AllowedTypePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
            }

			if (diagnostic.Id == ArchitecturalDiagnosticIds.ContractPurityViolation)
			{
				await ContractPurityCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.VisibilityPolicyViolation)
			{
				await VisibilityPolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id is ArchitecturalDiagnosticIds.ApiSurfaceLeakage or ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure)
			{
				await ApiSurfacePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation)
			{
				await ProjectArchitectureCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceViolation)
			{
				await PackagePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation)
			{
				await InheritancePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.SourceLocationViolation)
			{
				await SourceLocationCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id is ArchitecturalDiagnosticIds.IllegalLevelDependency or ArchitecturalDiagnosticIds.WrongDirectionDependency or ArchitecturalDiagnosticIds.SameLayerDependency)
			{
				await DependencyRuleCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.BoundaryEntryPointViolation)
			{
				await BoundaryEntryPointCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.CyclicDependencyGraph)
			{
				await CycleDependencyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id is ArchitecturalDiagnosticIds.IllegalLevelDependency
			    or ArchitecturalDiagnosticIds.ForbiddenDependency
			    or ArchitecturalDiagnosticIds.WrongDirectionDependency
			    or ArchitecturalDiagnosticIds.SameLayerDependency)
			{
				await AddToExceptionsCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}
		}
	}

	private static Task TryRegisterRenameAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var result = RenameCodeFix.TryRegisterAsync(context, diagnostic);

		return result;
	}
}
