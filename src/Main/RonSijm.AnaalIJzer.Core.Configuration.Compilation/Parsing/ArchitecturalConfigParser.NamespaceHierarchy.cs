using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
    internal static ImmutableArray<NamespaceHierarchyPolicy> ParseNamespaceHierarchyPolicies(IEnumerable<ArchitectureConfigurationElementInput> policyInputs, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var policies = ImmutableArray.CreateBuilder<NamespaceHierarchyPolicy>();
        foreach (var policyInput in policyInputs)
        {
            var element = policyInput.Element;
            if (element.Attributes().Any(attribute => attribute.Name.LocalName is not ("rootNamespace" or "description" or "comment")))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "NamespaceHierarchyPolicy supports rootNamespace, description, and comment attributes only.", element, policyInput.Path);

                continue;
            }

            if (!NamespaceHierarchyPath.TryParse(element.Attribute("rootNamespace")?.Value, out var rootPath))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "NamespaceHierarchyPolicy requires a non-empty dot-separated rootNamespace.", element, policyInput.Path);

                continue;
            }

            var rules = ParseNamespaceHierarchyRules(element, policyInput.Path, issues);
            if (rules.IsDefaultOrEmpty)
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "NamespaceHierarchyPolicy requires at least one BlockedRelation rule.", element, policyInput.Path);

                continue;
            }

            var line = (IXmlLineInfo)element;
            policies.Add(new NamespaceHierarchyPolicy(
                rootPath,
                rules,
                element.Attribute("description")?.Value,
                policyInput.Path,
                line.HasLineInfo() ? line.LineNumber : 0,
                line.HasLineInfo() ? line.LinePosition : 0));
        }

        var result = policies.ToImmutable();

        return result;
    }

    private static ImmutableArray<NamespaceHierarchyRule> ParseNamespaceHierarchyRules(XElement policyElement, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var rules = ImmutableArray.CreateBuilder<NamespaceHierarchyRule>();
        foreach (var element in policyElement.Elements())
        {
            if (element.Name.LocalName != "BlockedRelation")
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "NamespaceHierarchyPolicy supports BlockedRelation children only.", element, xmlPath);

                continue;
            }

            if (element.Attributes().Any(attribute => attribute.Name.LocalName is not ("relation" or "allowedSites" or "blockedSites" or "description" or "comment")))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "BlockedRelation supports relation, allowedSites, blockedSites, description, and comment attributes only.", element, xmlPath);

                continue;
            }

            if (!Enum.TryParse<NamespaceHierarchyRelation>(element.Attribute("relation")?.Value?.Trim(), true, out var relation)
                || !Enum.IsDefined(typeof(NamespaceHierarchyRelation), relation))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "BlockedRelation requires relation=\"DescendantToAncestor\", \"AncestorToDescendant\", \"SiblingToSibling\", or \"SameNamespace\".", element, xmlPath);

                continue;
            }

            if (!TryReadSiteFilter(element, out var siteFilter, out var error))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, error, element, xmlPath);

                continue;
            }

            var line = (IXmlLineInfo)element;
            rules.Add(new NamespaceHierarchyRule(
                relation,
                siteFilter,
                element.Attribute("description")?.Value,
                xmlPath,
                line.HasLineInfo() ? line.LineNumber : 0,
                line.HasLineInfo() ? line.LinePosition : 0));
        }

        var result = rules.ToImmutable();

        return result;
    }
}