using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Policies;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
    private static ImmutableArray<ForbiddenOperationPolicy> ParseForbiddenOperationPolicies(IEnumerable<XElement> containers, string ownerLayerPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var policies = ImmutableArray.CreateBuilder<ForbiddenOperationPolicy>();
        foreach (var container in containers)
        {
            if (HasUnexpectedAttributes(container, "description", "comment"))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ForbiddenOperations supports only description and comment attributes.", container, xmlPath);
                continue;
            }

            var rules = ImmutableArray.CreateBuilder<ForbiddenOperationRule>();
            foreach (var child in container.Elements())
            {
                if (child.Name.LocalName != "ForbiddenOperation")
                {
                    AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ForbiddenOperations supports only ForbiddenOperation children.", child, xmlPath);
                    continue;
                }

                if (!TryParseForbiddenOperationRule(child, xmlPath, issues, out var rule))
                {
                    continue;
                }

                rules.Add(rule);
            }

            if (rules.Count == 0)
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"ForbiddenOperations in layer '{ownerLayerPath}' requires at least one valid ForbiddenOperation.", container, xmlPath);
                continue;
            }

            var line = (IXmlLineInfo)container;
            var policy = new ForbiddenOperationPolicy(
                ownerLayerPath,
                rules.ToImmutable(),
                container.Attribute("description")?.Value,
                xmlPath,
                line.HasLineInfo() ? line.LineNumber : 0,
                line.HasLineInfo() ? line.LinePosition : 0);
            policies.Add(policy);
        }

        var result = policies.ToImmutable();

        return result;
    }

    private static bool TryParseForbiddenOperationRule(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out ForbiddenOperationRule rule)
    {
        if (HasUnexpectedAttributes(element, "description", "comment", "allowedSites", "blockedSites"))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ForbiddenOperation supports description, comment, allowedSites, and blockedSites attributes.", element, xmlPath);
            rule = default;
            return false;
        }

        if (!TryReadSiteFilter(element, out var siteFilter, out var siteFilterError))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, element, xmlPath);
            rule = default;
            return false;
        }

        var matchers = ImmutableArray.CreateBuilder<SemanticOperationMatcher>();
        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName != "OperationMatcher")
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ForbiddenOperation supports only OperationMatcher children.", child, xmlPath);
                continue;
            }

            if (!TryParseOperationMatcher(child, xmlPath, issues, out var matcher))
            {
                continue;
            }

            matchers.Add(matcher);
        }

        if (matchers.Count == 0)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ForbiddenOperation requires at least one valid OperationMatcher.", element, xmlPath);
            rule = default;
            return false;
        }

        var line = (IXmlLineInfo)element;
        rule = new ForbiddenOperationRule(
            matchers.ToImmutable(),
            siteFilter,
            CreateForbiddenOperationRuleDisplayName(element),
            element.Attribute("description")?.Value,
            xmlPath,
            line.HasLineInfo() ? line.LineNumber : 0,
            line.HasLineInfo() ? line.LinePosition : 0);
        return true;
    }

    private static bool TryParseOperationMatcher(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out SemanticOperationMatcher matcher)
    {
        if (HasUnexpectedAttributes(element, "kind", "staticAccess", "description", "comment"))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher supports kind, staticAccess, description, and comment attributes.", element, xmlPath);
            matcher = default;
            return false;
        }

        if (!TryParseOperationKinds(element.Attribute("kind")?.Value, out var kinds))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher requires a non-empty kind list containing known semantic operation kinds.", element, xmlPath);
            matcher = default;
            return false;
        }

        if (!TryReadOptionalBooleanAttribute(element, "staticAccess", out var staticAccess))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher staticAccess must be true, false, 1, or 0.", element, xmlPath);
            matcher = default;
            return false;
        }

        var unexpectedChildren = element.Elements().Where(child => child.Name.LocalName is not ("ContainingType" or "Member")).ToArray();
        if (unexpectedChildren.Length > 0)
        {
            foreach (var child in unexpectedChildren)
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher supports only ContainingType and Member children.", child, xmlPath);
            }

            matcher = default;
            return false;
        }

        var containingTypes = element.Elements("ContainingType").ToArray();
        var members = element.Elements("Member").ToArray();
        if (containingTypes.Length > 1 || members.Length > 1)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher permits at most one ContainingType and one Member matcher. Add another OperationMatcher for an alternative.", element, xmlPath);
            matcher = default;
            return false;
        }

        if (!TryParseOperationMatcherConditions(containingTypes.SingleOrDefault(), MatcherAttributeProfile.Type, false, "ContainingType", xmlPath, issues, out var containingTypeConditions, out _)
            || !TryParseOperationMatcherConditions(members.SingleOrDefault(), MatcherAttributeProfile.SemanticCodeObservation, true, "Member", xmlPath, issues, out var memberConditions, out var memberKinds))
        {
            matcher = default;
            return false;
        }

        if (!ValidateOperationMatcherMemberSelection(element, kinds, memberConditions, memberKinds, xmlPath, issues))
        {
            matcher = default;
            return false;
        }

        matcher = new SemanticOperationMatcher(kinds, containingTypeConditions, memberConditions, staticAccess, memberKinds);
        return true;
    }

    private static bool ValidateOperationMatcherMemberSelection(XElement element, ImmutableHashSet<SemanticOperationKind> kinds, ImmutableArray<MatchCondition> memberConditions, ImmutableHashSet<SemanticOperationMemberKind> memberKinds, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        if (memberConditions.IsDefaultOrEmpty && memberKinds.Count == 0)
        {
            return true;
        }

        var selectableKinds = ImmutableHashSet.CreateBuilder<SemanticOperationMemberKind>();
        var supportsMemberSelection = false;
        foreach (var kind in kinds)
        {
            switch (kind)
            {
                case SemanticOperationKind.Invocation:
                case SemanticOperationKind.Conversion:
                    selectableKinds.Add(SemanticOperationMemberKind.Method);
                    supportsMemberSelection = true;
                    break;
                case SemanticOperationKind.ObjectCreation:
                    selectableKinds.Add(SemanticOperationMemberKind.Constructor);
                    supportsMemberSelection = true;
                    break;
                case SemanticOperationKind.PropertyRead:
                case SemanticOperationKind.PropertyWrite:
                    selectableKinds.Add(SemanticOperationMemberKind.Property);
                    supportsMemberSelection = true;
                    break;
                case SemanticOperationKind.FieldRead:
                case SemanticOperationKind.FieldWrite:
                    selectableKinds.Add(SemanticOperationMemberKind.Field);
                    supportsMemberSelection = true;
                    break;
                case SemanticOperationKind.EventAccess:
                    selectableKinds.Add(SemanticOperationMemberKind.Event);
                    supportsMemberSelection = true;
                    break;
                case SemanticOperationKind.Assignment:
                    selectableKinds.UnionWith([SemanticOperationMemberKind.Property, SemanticOperationMemberKind.Field, SemanticOperationMemberKind.Event]);
                    supportsMemberSelection = true;
                    break;
            }
        }

        if (!supportsMemberSelection)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher kinds Return and Argument do not expose a selected member. Remove the Member child or choose a member-selecting operation kind.", element, xmlPath);
            return false;
        }

        if (memberKinds.Count > 0 && !memberKinds.IsSubsetOf(selectableKinds))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "OperationMatcher memberKind is incompatible with the configured operation kind.", element, xmlPath);
            return false;
        }

        return true;
    }

    private static bool TryParseOperationMatcherConditions(XElement? element, MatcherAttributeProfile profile, bool supportsMemberKind, string label, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out ImmutableArray<MatchCondition> conditions, out ImmutableHashSet<SemanticOperationMemberKind> memberKinds)
    {
        conditions = ImmutableArray<MatchCondition>.Empty;
        memberKinds = ImmutableHashSet<SemanticOperationMemberKind>.Empty;
        if (element is null)
        {
            return true;
        }

        if (element.Elements().Any())
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, label + " does not support child elements.", element, xmlPath);
            return false;
        }

        var hasUnsupportedAttribute = element.Attributes().Any(attribute => attribute.Name.LocalName is not ("description" or "comment" or "memberKind")
            && !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, profile));
        if (hasUnsupportedAttribute || (!supportsMemberKind && element.Attribute("memberKind") is not null))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, label + " contains an unsupported matcher attribute.", element, xmlPath);
            return false;
        }

        if (supportsMemberKind && !TryParseOperationMemberKinds(element.Attribute("memberKind")?.Value, out memberKinds))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Member memberKind must be a non-empty list of Method, Constructor, Property, Field, or Event values.", element, xmlPath);
            return false;
        }

        conditions = MatcherAttributeCatalog.CreateConditions(attributeName => element.Attribute(attributeName)?.Value, profile);
        if (conditions.IsDefaultOrEmpty && memberKinds.Count == 0)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, label + " requires at least one matcher attribute.", element, xmlPath);
            return false;
        }

        return true;
    }

    private static bool TryParseOperationKinds(string? text, out ImmutableHashSet<SemanticOperationKind> kinds)
    {
        var builder = ImmutableHashSet.CreateBuilder<SemanticOperationKind>();
        if (string.IsNullOrWhiteSpace(text))
        {
            kinds = ImmutableHashSet<SemanticOperationKind>.Empty;
            return false;
        }

        foreach (var token in text!.Split(','))
        {
            if (!SemanticOperationKindParser.TryParse(token, out var kind))
            {
                kinds = ImmutableHashSet<SemanticOperationKind>.Empty;
                return false;
            }

            builder.Add(kind);
        }

        kinds = builder.ToImmutable();
        return kinds.Count > 0;
    }

    private static bool TryParseOperationMemberKinds(string? text, out ImmutableHashSet<SemanticOperationMemberKind> kinds)
    {
        if (text is null)
        {
            kinds = ImmutableHashSet<SemanticOperationMemberKind>.Empty;
            return true;
        }

        var builder = ImmutableHashSet.CreateBuilder<SemanticOperationMemberKind>();
        foreach (var token in text.Split(','))
        {
            if (!SemanticOperationMemberKindParser.TryParse(token, out var kind))
            {
                kinds = ImmutableHashSet<SemanticOperationMemberKind>.Empty;
                return false;
            }

            builder.Add(kind);
        }

        kinds = builder.ToImmutable();
        return kinds.Count > 0;
    }

    private static bool TryReadOptionalBooleanAttribute(XElement element, string attributeName, out bool? value)
    {
        if (element.Attribute(attributeName) is null)
        {
            value = null;
            return true;
        }

        if (!TryReadBooleanAttribute(element, attributeName, out var booleanValue))
        {
            value = null;
            return false;
        }

        value = booleanValue;
        return true;
    }

    private static bool HasUnexpectedAttributes(XElement element, params string[] allowedAttributes)
    {
        var result = element.Attributes().Any(attribute => !allowedAttributes.Contains(attribute.Name.LocalName, StringComparer.Ordinal));

        return result;
    }

    private static string CreateForbiddenOperationRuleDisplayName(XElement element)
    {
        var kinds = element.Elements("OperationMatcher")
            .Select(matcher => matcher.Attribute("kind")?.Value)
            .Where(kind => !string.IsNullOrWhiteSpace(kind))
            .ToArray();
        var result = kinds.Length == 0 ? "configured operation" : string.Join(" or ", kinds) + " operation";

        return result;
    }
}