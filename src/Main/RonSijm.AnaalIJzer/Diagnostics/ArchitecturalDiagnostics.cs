using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Diagnostics;

/// <summary>
///     Diagnostic descriptors and diagnostic property key constants for the architectural analyzer.
/// </summary>
internal static class ArchitecturalDiagnostics
{
	// Property keys embedded in ARCH003 diagnostics so the code fix provider can read them.
	internal const string PropertyMatchedSuffix = "MatchedSuffix";
	internal const string PropertyFixSuffix = "FixSuffix";

	// Property key carrying the syntactic site of every dependency diagnostic.
	// See <see cref="DependencySites"/> for the closed set of values.
	internal const string PropertySite = "Site";

	// Property keys embedded in every diagnostic so the "Add to exceptions" code fix can
	// (a) locate the originating <Class>/<Namespace> tag in ArchitecturalLevels.xml and
	// (b) know which exact type name to insert as the new <Class typeName="..."/> exception.
	internal const string PropertyRuleXmlLine = "RuleXmlLine";
	internal const string PropertyRuleXmlCol = "RuleXmlCol";
	internal const string PropertyRuleXmlPath = "RuleXmlPath";
	internal const string PropertyDepTypeName = "DepTypeName";
	internal const string PropertyCallerTypeName = "CallerTypeName";
	internal const string PropertyCallerLayerName = "CallerLayerName";
	internal const string PropertyDepLayerName = "DepLayerName";
	internal const string PropertyViolationReason = "ViolationReason";
	internal const string PropertyComment = "Comment";

	private const string HelpLinkBase = "https://github.com/RonSijm/RonSijm.AnaalIJzer#";

	internal static readonly DiagnosticDescriptor IllegalDependency = new(ArchitecturalDiagnosticIds.IllegalLevelDependency, "Illegal architectural layer dependency", "'{0}' (layer {1}) may not depend on '{2}' (layer {3}): {4}", "Architecture", DiagnosticSeverity.Error, true, "No AllowedDependency edge permits this dependency site between the caller's layer and the dependency's layer.", HelpLinkBase + "arch001--illegal-layer-dependency");

	internal static readonly DiagnosticDescriptor UnrecognizedDependency = new(ArchitecturalDiagnosticIds.UnrecognizedDependency, "Unrecognized architectural dependency", "'{0}' (layer {1}) depends on '{2}' which is not assigned to any architectural layer{3}", "Architecture", DiagnosticSeverity.Error, true, "In strict mode, every dependency of a layered type must itself belong to a configured architectural layer.", HelpLinkBase + "arch002--unrecognized-dependency-strict-mode");

	internal static readonly DiagnosticDescriptor ForbiddenDependency = new(ArchitecturalDiagnosticIds.ForbiddenDependency, "Architectural type policy violation", "'{0}' (layer {1}) may not use '{2}': {3}", "Architecture", DiagnosticSeverity.Error, true, "The dependency matches a Forbidden type policy or fails an applicable Allowed type policy.", HelpLinkBase + "arch003--type-policy-violation");

	internal static readonly DiagnosticDescriptor WrongDirectionDependency = new(ArchitecturalDiagnosticIds.WrongDirectionDependency, "Wrong-direction architectural dependency", "'{0}' (layer {1}) may not depend on '{2}' (layer {3}): {4}", "Architecture", DiagnosticSeverity.Error, true, "The caller depends on a layer that is configured to depend on it — almost always an architectural mistake. Reverse the dependency or invert it with an abstraction.", HelpLinkBase + "arch004--wrong-direction-dependency");

	internal static readonly DiagnosticDescriptor SameLayerDependency = new(ArchitecturalDiagnosticIds.SameLayerDependency, "Same-layer architectural dependency", "'{0}' and '{2}' are both in layer '{1}': {4}", "Architecture", DiagnosticSeverity.Error, true, "Same-layer dependencies create hidden coupling within a layer. Extract the shared concept to a lower layer or merge the responsibilities.", HelpLinkBase + "arch005--same-layer-dependency");

	internal static readonly DiagnosticDescriptor InvalidConfiguration = new(ArchitecturalDiagnosticIds.InvalidConfiguration, "Invalid architecture configuration", "{0}", "Architecture", DiagnosticSeverity.Error, true, "The architecture configuration is malformed or refers to rules that cannot be evaluated.", HelpLinkBase + "arch006--invalid-architecture-configuration");

	internal static readonly DiagnosticDescriptor CyclicDependencyGraph = new(ArchitecturalDiagnosticIds.CyclicDependencyGraph, "Cyclic architecture dependency graph", "{0}", "Architecture", DiagnosticSeverity.Error, true, "The configured allowed-dependency graph contains a cycle while enforceAcyclic is enabled.", HelpLinkBase + "arch007--cyclic-architecture-dependency-graph");
}

/// <summary>
///     Closed set of values that may appear in <c>Diagnostic.Properties["Site"]</c>.
///     Downstream tooling (reports, dashboards, future code fixes) can branch on these.
/// </summary>
internal static class DependencySites
{
	internal const string Constructor = "Constructor";
	internal const string Method = "Method";
	internal const string MethodReturn = "MethodReturn";
	internal const string Field = "Field";
	internal const string Property = "Property";
	internal const string Local = "Local";
	internal const string New = "New";
	internal const string GenericInvocation = "GenericInvocation";
	internal const string GenericArgument = "GenericArgument";
	internal const string Inheritance = "Inheritance";
	internal const string Attribute = "Attribute";
	internal const string StaticMember = "StaticMember";

	internal static readonly string[] All =
	[
		Constructor,
		Method,
		MethodReturn,
		Field,
		Property,
		Local,
		New,
		GenericInvocation,
		GenericArgument,
		Inheritance,
		Attribute,
		StaticMember,
	];

	internal static bool TryNormalize(string value, out string normalized)
	{
		foreach (var site in All)
		{
			if (string.Equals(value, site, StringComparison.OrdinalIgnoreCase))
			{
				normalized = site;
				return true;
			}
		}

		normalized = string.Empty;
		return false;
	}
}
