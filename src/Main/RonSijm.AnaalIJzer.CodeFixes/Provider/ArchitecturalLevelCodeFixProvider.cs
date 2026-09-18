using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArchitecturalLevelCodeFixProvider))]
[Shared]
public sealed class ArchitecturalLevelCodeFixProvider : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds =>
	[
		ArchitecturalDiagnosticIds.DependencyNotAllowed,
		ArchitecturalDiagnosticIds.DependencyRequiredMissing,
		ArchitecturalDiagnosticIds.NameShapeMismatch,
		ArchitecturalDiagnosticIds.TypeNotAllowed,
		ArchitecturalDiagnosticIds.DependencyReverseDirection,
		ArchitecturalDiagnosticIds.DependencyPeerScope,
		ArchitecturalDiagnosticIds.ApiExposureNotAllowed,
		ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed,
		ArchitecturalDiagnosticIds.PackageReferenceNotAllowed,
		ArchitecturalDiagnosticIds.VisibilityNotAllowed,
		ArchitecturalDiagnosticIds.ContractShapeMismatch,
		ArchitecturalDiagnosticIds.ApiTransitiveExposure,
		ArchitecturalDiagnosticIds.InheritanceNotAllowed,
		ArchitecturalDiagnosticIds.SourceBoundaryPlacement,
		ArchitecturalDiagnosticIds.BoundaryEntryPlacement,
		ArchitecturalDiagnosticIds.ConfigurationCycle,
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
			if (diagnostic.Id == ArchitecturalDiagnosticIds.DependencyRequiredMissing)
			{
				await RecognizedDependencyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.NameShapeMismatch)
			{
				await DeclarationNameCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
				await NameRuleAllowMappingCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

            if (diagnostic.Id == ArchitecturalDiagnosticIds.TypeNotAllowed)
            {
                await TryRegisterRenameAsync(context, diagnostic).ConfigureAwait(false);
				await AllowedTypePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
            }

			if (diagnostic.Id == ArchitecturalDiagnosticIds.ContractShapeMismatch)
			{
				await ContractPurityCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.VisibilityNotAllowed)
			{
				await VisibilityPolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id is ArchitecturalDiagnosticIds.ApiExposureNotAllowed or ArchitecturalDiagnosticIds.ApiTransitiveExposure)
			{
				await ApiSurfacePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed)
			{
				await ProjectArchitectureCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceNotAllowed)
			{
				await PackagePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.InheritanceNotAllowed)
			{
				await InheritancePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.SourceBoundaryPlacement)
			{
				await SourceLocationCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id is ArchitecturalDiagnosticIds.DependencyNotAllowed or ArchitecturalDiagnosticIds.DependencyReverseDirection or ArchitecturalDiagnosticIds.DependencyPeerScope)
			{
				await DependencyRuleCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.BoundaryEntryPlacement)
			{
				await BoundaryEntryPointCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.ConfigurationCycle)
			{
				await CycleDependencyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id is ArchitecturalDiagnosticIds.DependencyNotAllowed
			    or ArchitecturalDiagnosticIds.TypeNotAllowed
			    or ArchitecturalDiagnosticIds.DependencyReverseDirection
			    or ArchitecturalDiagnosticIds.DependencyPeerScope)
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
