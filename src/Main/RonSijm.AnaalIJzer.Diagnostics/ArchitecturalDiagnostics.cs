using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class ArchitecturalDiagnostics
{
	internal const string PropertyDiagnosticConcern = ArchitectureDiagnosticProperties.PropertyDiagnosticConcern;
	internal const string PropertyDiagnosticReason = ArchitectureDiagnosticProperties.PropertyDiagnosticReason;
	internal const string PropertyMatchedSuffix = ArchitectureDiagnosticProperties.PropertyMatchedSuffix;
	internal const string PropertyFixSuffix = ArchitectureDiagnosticProperties.PropertyFixSuffix;
	internal const string PropertySite = ArchitectureDiagnosticProperties.PropertySite;
	internal const string PropertyRuleXmlLine = ArchitectureDiagnosticProperties.PropertyRuleXmlLine;
	internal const string PropertyRuleXmlCol = ArchitectureDiagnosticProperties.PropertyRuleXmlCol;
	internal const string PropertyRuleXmlPath = ArchitectureDiagnosticProperties.PropertyRuleXmlPath;
	internal const string PropertyDepTypeName = ArchitectureDiagnosticProperties.PropertyDepTypeName;
	internal const string PropertyCallerTypeName = ArchitectureDiagnosticProperties.PropertyCallerTypeName;
	internal const string PropertyCallerLayerName = ArchitectureDiagnosticProperties.PropertyCallerLayerName;
	internal const string PropertyDepLayerName = ArchitectureDiagnosticProperties.PropertyDepLayerName;
	internal const string PropertyViolationReason = ArchitectureDiagnosticProperties.PropertyViolationReason;
	internal const string PropertyComment = ArchitectureDiagnosticProperties.PropertyComment;
	internal const string PropertyDependencyDenialKind = ArchitectureDiagnosticProperties.PropertyDependencyDenialKind;
	internal const string PropertyDependencySiteFilterMode = ArchitectureDiagnosticProperties.PropertyDependencySiteFilterMode;
	internal const string PropertyDependencyRuleKind = ArchitectureDiagnosticProperties.PropertyDependencyRuleKind;
	internal const string PropertyDependencyRuleXmlLine = ArchitectureDiagnosticProperties.PropertyDependencyRuleXmlLine;
	internal const string PropertyDependencyRuleXmlCol = ArchitectureDiagnosticProperties.PropertyDependencyRuleXmlCol;
	internal const string PropertyDependencyRuleXmlPath = ArchitectureDiagnosticProperties.PropertyDependencyRuleXmlPath;
	internal const string PropertyDependencyRuleScopePath = ArchitectureDiagnosticProperties.PropertyDependencyRuleScopePath;
	internal const string PropertyDependencyRuleConfiguredFrom = ArchitectureDiagnosticProperties.PropertyDependencyRuleConfiguredFrom;
	internal const string PropertyDependencyRuleConfiguredTo = ArchitectureDiagnosticProperties.PropertyDependencyRuleConfiguredTo;
	internal const string PropertyReverseDependencyRuleKind = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleKind;
	internal const string PropertyReverseDependencyRuleXmlLine = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleXmlLine;
	internal const string PropertyReverseDependencyRuleXmlCol = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleXmlCol;
	internal const string PropertyReverseDependencyRuleXmlPath = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleXmlPath;
	internal const string PropertyReverseDependencyRuleScopePath = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleScopePath;
	internal const string PropertyReverseDependencyRuleConfiguredFrom = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleConfiguredFrom;
	internal const string PropertyReverseDependencyRuleConfiguredTo = ArchitectureDiagnosticProperties.PropertyReverseDependencyRuleConfiguredTo;
	internal const string PropertySourceName = ArchitectureDiagnosticProperties.PropertySourceName;
	internal const string PropertyTargetName = ArchitectureDiagnosticProperties.PropertyTargetName;
	internal const string PropertySourceProjectPath = ArchitectureDiagnosticProperties.PropertySourceProjectPath;
	internal const string PropertySourceProjectName = ArchitectureDiagnosticProperties.PropertySourceProjectName;
	internal const string PropertySourceProjectGroup = ArchitectureDiagnosticProperties.PropertySourceProjectGroup;
	internal const string PropertyTargetProjectPath = ArchitectureDiagnosticProperties.PropertyTargetProjectPath;
	internal const string PropertyTargetProjectName = ArchitectureDiagnosticProperties.PropertyTargetProjectName;
	internal const string PropertyTargetProjectGroup = ArchitectureDiagnosticProperties.PropertyTargetProjectGroup;
	internal const string PropertyPackageId = ArchitectureDiagnosticProperties.PropertyPackageId;
	internal const string PropertyPackageVersion = ArchitectureDiagnosticProperties.PropertyPackageVersion;
	internal const string PropertyPackageReferenceKind = ArchitectureDiagnosticProperties.PropertyPackageReferenceKind;
	internal const string PropertySourceFilePath = ArchitectureDiagnosticProperties.PropertySourceFilePath;
	internal const string PropertyNormalizedSourcePath = ArchitectureDiagnosticProperties.PropertyNormalizedSourcePath;
	internal const string PropertySourceAssemblyName = ArchitectureDiagnosticProperties.PropertySourceAssemblyName;
	internal const string PropertyNormalizedSourceName = ArchitectureDiagnosticProperties.PropertyNormalizedSourceName;
	internal const string PropertyNormalizedTargetName = ArchitectureDiagnosticProperties.PropertyNormalizedTargetName;
	internal const string PropertyNameRuleKind = ArchitectureDiagnosticProperties.PropertyNameRuleKind;
	internal const string PropertyTypeName = ArchitectureDiagnosticProperties.PropertyTypeName;
	internal const string PropertyDeclaredName = ArchitectureDiagnosticProperties.PropertyDeclaredName;
	internal const string PropertyDeclaredSymbolName = ArchitectureDiagnosticProperties.PropertyDeclaredSymbolName;
	internal const string PropertyDeclarationTarget = ArchitectureDiagnosticProperties.PropertyDeclarationTarget;
	internal const string PropertyDeclaredAccessibility = ArchitectureDiagnosticProperties.PropertyDeclaredAccessibility;
	internal const string PropertyApiMemberName = ArchitectureDiagnosticProperties.PropertyApiMemberName;
	internal const string PropertyExposureRootMember = ArchitectureDiagnosticProperties.PropertyExposureRootMember;
	internal const string PropertyExposurePath = ArchitectureDiagnosticProperties.PropertyExposurePath;
	internal const string PropertyExposureDepth = ArchitectureDiagnosticProperties.PropertyExposureDepth;
	internal const string PropertyNestedMemberName = ArchitectureDiagnosticProperties.PropertyNestedMemberName;
	internal const string PropertyNestedMemberContainingType = ArchitectureDiagnosticProperties.PropertyNestedMemberContainingType;
	internal const string PropertyContractViolationKind = ArchitectureDiagnosticProperties.PropertyContractViolationKind;
	internal const string PropertyContractPropertyAccessor = ArchitectureDiagnosticProperties.PropertyContractPropertyAccessor;
	internal const string PropertyInheritanceViolationKind = ArchitectureDiagnosticProperties.PropertyInheritanceViolationKind;
	internal const string PropertyReturnValueRuleTarget = ArchitectureDiagnosticProperties.PropertyReturnValueRuleTarget;
	internal const string PropertyReturnValueRule = ArchitectureDiagnosticProperties.PropertyReturnValueRule;
	internal const string PropertyReturnValueRuleMode = ArchitectureDiagnosticProperties.PropertyReturnValueRuleMode;
	internal const string PropertyOperationKind = ArchitectureDiagnosticProperties.PropertyOperationKind;
	internal const string PropertyOperationDisplayName = ArchitectureDiagnosticProperties.PropertyOperationDisplayName;
	internal const string PropertyOperationPolicyRule = ArchitectureDiagnosticProperties.PropertyOperationPolicyRule;
	internal const string PropertyBehavioralOperationViolationKind = ArchitectureDiagnosticProperties.PropertyBehavioralOperationViolationKind;
	internal const string PropertyBehavioralOperationOrdering = ArchitectureDiagnosticProperties.PropertyBehavioralOperationOrdering;
	internal const string PropertyOperationContractName = ArchitectureDiagnosticProperties.PropertyOperationContractName;
	internal const string PropertyOperationContractParticipantRole = ArchitectureDiagnosticProperties.PropertyOperationContractParticipantRole;
	internal const string PropertyOperationContractViolationKind = ArchitectureDiagnosticProperties.PropertyOperationContractViolationKind;
	internal const string PropertyAssemblyAttributeTypeName = ArchitectureDiagnosticProperties.PropertyAssemblyAttributeTypeName;
	internal const string PropertyAssemblyAttributePolicyRule = ArchitectureDiagnosticProperties.PropertyAssemblyAttributePolicyRule;
	internal const string PropertyRequiredInheritanceTypeName = ArchitectureDiagnosticProperties.PropertyRequiredInheritanceTypeName;
	internal const string PropertyBoundaryLayerName = ArchitectureDiagnosticProperties.PropertyBoundaryLayerName;
	internal const string PropertyMatchedEntryPoint = ArchitectureDiagnosticProperties.PropertyMatchedEntryPoint;
	internal const string PropertyEntryPointFailureReason = ArchitectureDiagnosticProperties.PropertyEntryPointFailureReason;
	internal const string PropertyCycleLayers = ArchitectureDiagnosticProperties.PropertyCycleLayers;
	internal const string PropertyCycleLength = ArchitectureDiagnosticProperties.PropertyCycleLength;
	internal const string PropertyObservedSites = ArchitectureDiagnosticProperties.PropertyObservedSites;
	internal const string PropertyCycleScope = ArchitectureDiagnosticProperties.PropertyCycleScope;
	internal const string PropertyCycleRuleCandidates = ArchitectureDiagnosticProperties.PropertyCycleRuleCandidates;
	internal const string PropertyExceptionMatcherKind = ArchitectureDiagnosticProperties.PropertyExceptionMatcherKind;
	internal const string PropertyExceptionMatcherLabel = ArchitectureDiagnosticProperties.PropertyExceptionMatcherLabel;
	internal const string PropertyExceptionReason = ArchitectureDiagnosticProperties.PropertyExceptionReason;
	internal const string PropertyExceptionOwner = ArchitectureDiagnosticProperties.PropertyExceptionOwner;
	internal const string PropertyExceptionExpiresOn = ArchitectureDiagnosticProperties.PropertyExceptionExpiresOn;
	internal const string PropertyExceptionStatus = ArchitectureDiagnosticProperties.PropertyExceptionStatus;
	internal const string PropertyCallerNamespace = ArchitectureDiagnosticProperties.PropertyCallerNamespace;
	internal const string PropertyDependencyNamespace = ArchitectureDiagnosticProperties.PropertyDependencyNamespace;
	internal const string PropertyNamespaceHierarchyRoot = ArchitectureDiagnosticProperties.PropertyNamespaceHierarchyRoot;
	internal const string PropertyNamespaceHierarchyRelation = ArchitectureDiagnosticProperties.PropertyNamespaceHierarchyRelation;
	internal const string PropertyNamespaceHierarchyRuleXmlPath = ArchitectureDiagnosticProperties.PropertyNamespaceHierarchyRuleXmlPath;
	internal const string PropertyNamespaceHierarchyRuleXmlLine = ArchitectureDiagnosticProperties.PropertyNamespaceHierarchyRuleXmlLine;
	internal const string PropertyNamespaceHierarchyRuleXmlCol = ArchitectureDiagnosticProperties.PropertyNamespaceHierarchyRuleXmlCol;

	private const string HelpLinkBase = "https://github.com/RonSijm/RonSijm.AnaalIJzer/blob/main/docs/";

	internal static readonly DiagnosticDescriptor DependencyNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.DependencyNotAllowed, "'{0}' (layer {1}) may not depend on '{2}' (layer {3}): {4}", "No AllowedDependency edge permits this dependency site between the caller's layer and the dependency's layer.");
	internal static readonly DiagnosticDescriptor DependencyRequiredMissing = CreateDescriptor(ArchitectureDiagnosticCatalog.DependencyRequiredMissing, "'{0}' (layer {1}) depends on '{2}' which is not assigned to any architectural layer{3}", "When requireRecognizedDependencies includes a dependency site, types used at that site must belong to a configured architectural layer.");
	internal static readonly DiagnosticDescriptor TypeNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.TypeNotAllowed, "'{0}' (layer {1}) may not use '{2}': {3}", "The dependency matches a Forbidden type policy or fails an applicable Allowed type policy.");
	internal static readonly DiagnosticDescriptor DependencyReverseDirection = CreateDescriptor(ArchitectureDiagnosticCatalog.DependencyReverseDirection, "'{0}' (layer {1}) may not depend on '{2}' (layer {3}): {4}", "The caller depends on a layer that is configured to depend on it. Reverse the dependency or invert it with an abstraction.");
	internal static readonly DiagnosticDescriptor DependencyPeerScope = CreateDescriptor(ArchitectureDiagnosticCatalog.DependencyPeerScope, "'{0}' and '{2}' are both in layer '{1}': {4}", "Same-layer dependencies create hidden coupling within a layer. Extract the shared concept to a lower layer or merge the responsibilities.");
	internal static readonly DiagnosticDescriptor ConfigurationInvalid = CreateDescriptor(ArchitectureDiagnosticCatalog.ConfigurationInvalid, "{0}", "The architecture configuration is malformed or refers to rules that cannot be evaluated.", "CompilationEnd");
	internal static readonly DiagnosticDescriptor ConfigurationCycle = CreateDescriptor(ArchitectureDiagnosticCatalog.ConfigurationCycle, "{0}", "The configured allowed-dependency graph contains a cycle while enforceAcyclic is enabled.");
	internal static readonly DiagnosticDescriptor NameShapeMismatch = CreateDescriptor(ArchitectureDiagnosticCatalog.NameShapeMismatch, "'{0}' (layer {1}) violates name rule '{2}' at {3}: {4}", "Layer-scoped NameRules compare meaningful names with other value names or with their declared semantic types unless an explicit Allow mapping permits the translation.");
	internal static readonly DiagnosticDescriptor ApiExposureNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.ApiExposureNotAllowed, "'{0}' (layer {1}) exposes '{2}' (layer {3}) at {4}: {5}", "A layer-scoped ApiSurface policy rejected a type exposed by an externally visible declaration.");
	internal static readonly DiagnosticDescriptor ProjectReferenceNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.ProjectReferenceNotAllowed, "Project '{0}' (project group {1}) may not reference project '{2}' (project group {3}): {4}", "A ProjectArchitecture policy rejected a direct MSBuild project reference, including currently unused references.", "CompilationEnd");
	internal static readonly DiagnosticDescriptor PackageReferenceNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.PackageReferenceNotAllowed, "Project '{0}' (project group {1}) may not reference package '{2}' {3}: {4}", "A ProjectArchitecture PackagePolicy rejected a direct or transitive resolved NuGet package reference.", "CompilationEnd");
	internal static readonly DiagnosticDescriptor VisibilityNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.VisibilityNotAllowed, "'{0}' (layer {1}) is declared {2}: {3}", "A layer-scoped VisibilityPolicy rejected the declared accessibility of a type or member.");
	internal static readonly DiagnosticDescriptor ContractShapeMismatch = CreateDescriptor(ArchitectureDiagnosticCatalog.ContractShapeMismatch, "'{0}' (layer {1}) violates contract purity at {2}: {3}", "A layer-scoped ContractPolicy rejected the declaration shape of a contract type or member.");
	internal static readonly DiagnosticDescriptor ApiTransitiveExposure = CreateDescriptor(ArchitectureDiagnosticCatalog.ApiTransitiveExposure, "'{0}' (layer {1}) transitively exposes '{2}' (layer {3}) through {4}: {5}", "A layer-scoped ApiSurface policy rejected a type reachable through the externally visible object graph of an exposed type.");
	internal static readonly DiagnosticDescriptor SourceBoundaryPlacement = CreateDescriptor(ArchitectureDiagnosticCatalog.SourceBoundaryPlacement, "'{0}' belongs to layer '{1}' but source file '{2}' does not match an allowed SourceLocations rule for layer '{3}'", "A layer-scoped SourceLocations policy rejected the physical file location of a declaration classified into that layer.");
	internal static readonly DiagnosticDescriptor BoundaryEntryPlacement = CreateDescriptor(ArchitectureDiagnosticCatalog.BoundaryEntryPlacement, "'{0}' (layer {1}) may not enter boundary '{4}' through '{2}' (layer {3}): {5}", "A boundary EntryPoints policy rejected an otherwise allowed dependency because it enters the boundary through a non-entry layer or type.");
	internal static readonly DiagnosticDescriptor ExceptionReviewLifecycle = CreateDescriptor(ArchitectureDiagnosticCatalog.ExceptionReviewLifecycle, "{0}", "A configured architecture exception is missing required metadata, has expired, or is close to expiry.", "CompilationEnd");
	internal static readonly DiagnosticDescriptor DependencyCycle = CreateDescriptor(ArchitectureDiagnosticCatalog.DependencyCycle, "Observed architectural dependency cycle: {0}", "Observed source dependencies currently form a cycle between configured layers.", "CompilationEnd");
	internal static readonly DiagnosticDescriptor InheritanceNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.InheritanceNotAllowed, "'{0}' (layer {1}) violates inheritance policy at {2}: {3}", "A layer-scoped InheritancePolicy rejected the declared base-type or interface contract of a type.");
	internal static readonly DiagnosticDescriptor ReturnNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.ReturnNotAllowed, "'{0}' (layer {1}) violates return-value policy at {2}: {3}", "A global or layer-scoped ReturnValuePolicy rejected a direct method return expression.");
	internal static readonly DiagnosticDescriptor OperationNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationNotAllowed, "'{0}' (layer {1}) may not use operation '{2}' at {3}: {4}", "A layer-scoped ForbiddenOperations policy rejected a resolved semantic operation.");
	internal static readonly DiagnosticDescriptor OperationRequiredMissing = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationRequiredMissing, "'{0}' (layer {1}) is missing required operation '{2}' at {3}: {4}", "A layer-scoped BehavioralOperations policy requires an operation that is absent from the selected declaration.");
	internal static readonly DiagnosticDescriptor OperationCardinality = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationCardinality, "'{0}' (layer {1}) exceeds the allowed count for operation '{2}' at {3}: {4}", "A layer-scoped BehavioralOperations policy rejected the number of matching operation occurrences.");
	internal static readonly DiagnosticDescriptor OperationOrdering = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationOrdering, "'{0}' (layer {1}) violates operation ordering for '{2}' at {3}: {4}", "A layer-scoped BehavioralOperations policy rejected a mechanically provable operation ordering.");
	internal static readonly DiagnosticDescriptor OperationContractNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationContractNotAllowed, "'{0}' (layer {1}) is not allowed by operation contract '{2}' as {3}: {4}", "An explicitly configured operation contract rejected the layer of a selected owner or entry point.");
	internal static readonly DiagnosticDescriptor OperationContractRequiredMissing = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationContractRequiredMissing, "'{0}' (layer {1}) is missing a requirement of operation contract '{2}' as {3}: {4}", "An explicitly configured operation contract requires a request or owner invocation that is absent.");
	internal static readonly DiagnosticDescriptor OperationContractShapeMismatch = CreateDescriptor(ArchitectureDiagnosticCatalog.OperationContractShapeMismatch, "'{0}' (layer {1}) has an invalid response shape for operation contract '{2}' as {3}: {4}", "An explicitly configured operation contract rejected the response shape of a selected owner or entry point.");
	internal static readonly DiagnosticDescriptor AssemblyAttributeNotAllowed = CreateDescriptor(ArchitectureDiagnosticCatalog.AssemblyAttributeNotAllowed, "Assembly '{0}' may not declare attribute '{1}': {2}", "A root-level AssemblyAttributePolicy rejected an assembly attribute or one of its configured argument values.", "CompilationEnd");
	internal static readonly DiagnosticDescriptor NamespaceBoundaryPlacement = CreateDescriptor(ArchitectureDiagnosticCatalog.NamespaceBoundaryPlacement, "'{0}' (namespace '{1}') may not depend on '{2}' (namespace '{3}') at {4}: {5}", "A global NamespaceHierarchyPolicy rejected a resolved dependency relationship between source namespaces.");

	internal static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
	{
		var result = Diagnostic.Create(descriptor, location, AddIdentity(ImmutableDictionary<string, string?>.Empty, descriptor), messageArgs);

		return result;
	}

	internal static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs)
	{
		var result = Diagnostic.Create(descriptor, location, AddIdentity(properties, descriptor), messageArgs);

		return result;
	}

	internal static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, IEnumerable<Location> additionalLocations, ImmutableDictionary<string, string?> properties, params object[] messageArgs)
	{
		var result = Diagnostic.Create(descriptor, location, additionalLocations, AddIdentity(properties, descriptor), messageArgs);

		return result;
	}

	private static DiagnosticDescriptor CreateDescriptor(ArchitectureDiagnosticDefinition definition, string messageFormat, string description, params string[] customTags)
	{
		var result = new DiagnosticDescriptor(definition.Id, definition.Title, messageFormat, definition.Category, ToDiagnosticSeverity(definition.DefaultSeverity), true, description, HelpLinkBase + definition.DocumentationPath + ".md", customTags);

		return result;
	}

	private static ImmutableDictionary<string, string?> AddIdentity(ImmutableDictionary<string, string?> properties, DiagnosticDescriptor descriptor)
	{
		var definition = ArchitectureDiagnosticCatalog.Get(descriptor.Id);
		var result = properties
			.SetItem(PropertyDiagnosticConcern, definition.Concern.ToString())
			.SetItem(PropertyDiagnosticReason, definition.Reason.ToString());

		return result;
	}

	private static DiagnosticSeverity ToDiagnosticSeverity(ArchitectureDiagnosticDefaultSeverity severity)
	{
		var result = severity switch
		{
			ArchitectureDiagnosticDefaultSeverity.Info => DiagnosticSeverity.Info,
			ArchitectureDiagnosticDefaultSeverity.Warning => DiagnosticSeverity.Warning,
			_ => DiagnosticSeverity.Error
		};

		return result;
	}
}
