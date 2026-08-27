using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphApplication;

internal interface IArchitectureGraphEditService
{
	ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle);

	ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source);

	ArchitectureConfigurationEditResult SetRootSettings(
		ArchitectureConfigurationSource source,
		string? description,
		string? requireRecognizedDependencies,
		bool enforceAcyclic,
		bool enableReport,
		string? reportPath,
		bool enableDocumentation,
		string? documentationPath,
		bool enableExceptionPolicy,
		bool requireExceptionReason,
		bool requireExceptionOwner,
		bool requireExceptionExpiresOn,
		int exceptionWarnBeforeDays,
		string? exceptionPolicyDescription);

	ArchitectureConfigurationEditResult SetConfigurationElementAttributes(ArchitectureConfigurationElementEditHandle handle, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult SetConfigurationElementChildren(ArchitectureConfigurationElementEditHandle handle, string childXml);

	ArchitectureConfigurationEditResult RemoveConfigurationElement(ArchitectureConfigurationElementEditHandle handle);

	ArchitectureConfigurationEditResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult AddInclude(ArchitectureConfigurationSource source, string path);

	ArchitectureConfigurationEditResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description);

	ArchitectureConfigurationEditResult SetLayerName(ArchitectureLayerEditHandle handle, string name);

	ArchitectureConfigurationEditResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites);

	ArchitectureConfigurationEditResult RemoveLayer(ArchitectureLayerEditHandle handle);

	ArchitectureConfigurationEditResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath);

	ArchitectureConfigurationEditResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes);

	ArchitectureConfigurationEditResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult AddNameRule(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult AddInheritancePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult AddVisibilityPolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes);

	ArchitectureConfigurationEditResult AddApiSurfacePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml);

	ArchitectureConfigurationEditResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle);

	ArchitectureConfigurationEditResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites);

	ArchitectureConfigurationEditResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind);

	ArchitectureConfigurationEditResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants);

	ArchitectureConfigurationEditResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description);

	ArchitectureConfigurationEditResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to);

	ArchitectureConfigurationEditResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind);

	ArchitectureConfigurationEditResult CreateConfiguration(ArchitectureConfigurationCreationTarget target);

	ArchitectureConfigurationEditResult CreateConfiguration(ArchitectureConfigurationSource source);

	ArchitectureConfigurationEditResult ReadConfiguration(ArchitectureConfigurationSource source, out System.Xml.Linq.XDocument? document);
}
