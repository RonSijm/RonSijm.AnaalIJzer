using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
    internal static OperationContractCatalog ParseOperationContracts(IEnumerable<ArchitectureConfigurationElementInput> containers, IReadOnlyDictionary<string, LayerNode> layersByPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var definitions = ImmutableArray.CreateBuilder<OperationContractDefinition>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var containerInput in containers)
        {
            var container = containerInput.Element;
            if (HasUnexpectedAttributes(container, "description", "comment"))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operations supports only description and comment attributes.", container, containerInput.Path);
                continue;
            }

            if (!container.Elements().Any(child => child.Name.LocalName == "Operation"))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operations requires at least one Operation child.", container, containerInput.Path);
                continue;
            }

            foreach (var child in container.Elements())
            {
                if (child.Name.LocalName != "Operation")
                {
                    AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operations supports only Operation children.", child, containerInput.Path);
                    continue;
                }

                if (!TryParseOperationContract(child, containerInput.Path, layersByPath, issues, out var definition))
                {
                    continue;
                }

                if (!names.Add(definition.Name))
                {
                    AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Operation '{definition.Name}' is declared more than once.", child, containerInput.Path);
                    continue;
                }

                definitions.Add(definition);
            }
        }

        var result = new OperationContractCatalog(definitions.ToImmutable());

        return result;
    }

    private static bool TryParseOperationContract(XElement element, string xmlPath, IReadOnlyDictionary<string, LayerNode> layersByPath, ImmutableArray<ConfigurationIssue>.Builder issues, out OperationContractDefinition definition)
    {
        if (HasUnexpectedAttributes(element, "name", "allowedOwnerLayers", "allowedEntryPointLayers", "description", "comment"))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operation supports name, allowedOwnerLayers, allowedEntryPointLayers, description, and comment attributes.", element, xmlPath);
            definition = default;

            return false;
        }

        var name = element.Attribute("name")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operation requires a non-empty name.", element, xmlPath);
            definition = default;

            return false;
        }

        if (!TryParseOperationContractOwner(element, xmlPath, issues, out var owner)
            || !TryParseOperationContractEntryPoints(element, xmlPath, issues, out var entryPoints)
            || !TryParseOperationContractTypeMatcher(element, "Request", xmlPath, issues, out var requestMatcher)
            || !TryParseOperationContractTypeMatcher(element, "Response", xmlPath, issues, out var responseMatcher)
            || !TryParseOperationContractLayers(element, "allowedOwnerLayers", layersByPath, xmlPath, issues, out var allowedOwnerLayers)
            || !TryParseOperationContractLayers(element, "allowedEntryPointLayers", layersByPath, xmlPath, issues, out var allowedEntryPointLayers)
            || !HasOnlyOperationContractChildren(element, xmlPath, issues))
        {
            definition = default;

            return false;
        }

        var line = (IXmlLineInfo)element;
        definition = new OperationContractDefinition(
            name!,
            owner,
            entryPoints,
            requestMatcher,
            responseMatcher,
            allowedOwnerLayers,
            allowedEntryPointLayers,
            element.Attribute("description")?.Value,
            xmlPath,
            line.HasLineInfo() ? line.LineNumber : 0,
            line.HasLineInfo() ? line.LinePosition : 0);

        return true;
    }

    private static bool TryParseOperationContractOwner(XElement operation, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out OperationContractDeclarationSelector owner)
    {
        var owners = operation.Elements("Owner").ToArray();
        if (owners.Length != 1)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operation requires exactly one Owner.", operation, xmlPath);
            owner = default;

            return false;
        }

        var result = TryParseOperationContractDeclarationSelector(owners[0], "Owner", xmlPath, issues, out owner);

        return result;
    }

    private static bool TryParseOperationContractEntryPoints(XElement operation, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out ImmutableArray<OperationContractDeclarationSelector> entryPoints)
    {
        var selectors = ImmutableArray.CreateBuilder<OperationContractDeclarationSelector>();
        foreach (var entryPoint in operation.Elements("EntryPoint"))
        {
            if (!TryParseOperationContractDeclarationSelector(entryPoint, "EntryPoint", xmlPath, issues, out var selector))
            {
                entryPoints = ImmutableArray<OperationContractDeclarationSelector>.Empty;

                return false;
            }

            selectors.Add(selector);
        }

        entryPoints = selectors.ToImmutable();

        return true;
    }

    private static bool TryParseOperationContractDeclarationSelector(XElement container, string label, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out OperationContractDeclarationSelector selector)
    {
        if (HasUnexpectedAttributes(container, "description", "comment") || container.Elements().Any(child => child.Name.LocalName != "DeclarationMatcher"))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, label + " supports one DeclarationMatcher and documentation attributes.", container, xmlPath);
            selector = default;

            return false;
        }

        if (!TryParseBehavioralDeclarationMatcher(container, xmlPath, issues, out var matcher))
        {
            selector = default;

            return false;
        }

        if (!matcher.MemberKinds.SetEquals([SemanticOperationMemberKind.Method]))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, label + " DeclarationMatcher must select memberKind=\"Method\".", container, xmlPath);
            selector = default;

            return false;
        }

        var declaration = container.Element("DeclarationMatcher")!;
        var member = declaration.Element("Member");
        var displayName = member?.Attribute("exactName")?.Value
            ?? member?.Attribute("typeName")?.Value
            ?? member?.Attribute("endsWith")?.Value
            ?? label + " method";
        var line = (IXmlLineInfo)container;
        selector = new OperationContractDeclarationSelector(
            matcher,
            displayName,
            xmlPath,
            line.HasLineInfo() ? line.LineNumber : 0,
            line.HasLineInfo() ? line.LinePosition : 0);

        return true;
    }

    private static bool TryParseOperationContractTypeMatcher(XElement operation, string elementName, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out PatternMatcher? matcher)
    {
        var containers = operation.Elements(elementName).ToArray();
        if (containers.Length == 0)
        {
            matcher = null;

            return true;
        }

        if (containers.Length > 1)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Operation permits at most one {elementName}.", containers[1], xmlPath);
            matcher = null;

            return false;
        }

        var container = containers[0];
        if (HasUnexpectedAttributes(container, "description", "comment") || container.Elements().Any(child => child.Name.LocalName != "Class"))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, elementName + " supports one Class matcher and documentation attributes.", container, xmlPath);
            matcher = null;

            return false;
        }

        var classes = container.Elements("Class").ToArray();
        if (classes.Length != 1 || !ArchitectureConfigurationMatcherReader.TryReadMatcher(classes.SingleOrDefault()!, MatchTarget.TypeName, out var parsedMatcher))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, elementName + " requires exactly one Class matcher with at least one matcher attribute.", container, xmlPath);
            matcher = null;

            return false;
        }

        matcher = parsedMatcher;

        return true;
    }

    private static bool TryParseOperationContractLayers(XElement operation, string attributeName, IReadOnlyDictionary<string, LayerNode> layersByPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out ImmutableHashSet<string> layers)
    {
        var value = operation.Attribute(attributeName)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            layers = ImmutableHashSet<string>.Empty;

            return true;
        }

        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var token in value!.Split(','))
        {
            var reference = token.Trim();
            if (reference.Length == 0 || reference == "*" || !TryResolveOperationContractLayerReference(reference, layersByPath, out var resolvedLayer))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Operation {attributeName} references unknown layer '{reference}'. Use a root layer name or a root-qualified path starting with '/'.", operation, xmlPath);
                layers = ImmutableHashSet<string>.Empty;

                return false;
            }

            builder.Add(resolvedLayer);
        }

        layers = builder.ToImmutable();

        return true;
    }

    private static bool TryResolveOperationContractLayerReference(string reference, IReadOnlyDictionary<string, LayerNode> layersByPath, out string resolvedLayer)
    {
        if (reference.StartsWith("/", StringComparison.Ordinal))
        {
            resolvedLayer = reference.TrimStart('/');
        }
        else if (reference.Contains('/'))
        {
            resolvedLayer = string.Empty;

            return false;
        }
        else
        {
            resolvedLayer = reference;
        }

        var result = layersByPath.ContainsKey(resolvedLayer);

        return result;
    }

    private static bool HasOnlyOperationContractChildren(XElement operation, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        foreach (var child in operation.Elements())
        {
            if (child.Name.LocalName is "Owner" or "EntryPoint" or "Request" or "Response")
            {
                continue;
            }

            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Operation supports Owner, EntryPoint, Request, and Response children.", child, xmlPath);

            return false;
        }

        return true;
    }
}