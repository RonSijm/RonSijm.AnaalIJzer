using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
    internal static SolutionTopologyConfig ParseSolutionTopology(
        IEnumerable<ArchitectureConfigurationElementInput> elements,
        string configPath,
        ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var topologyInputs = elements.Where(item => item.Element.Name.LocalName == "SolutionTopology").ToArray();
        if (topologyInputs.Length == 0)
        {
            return SolutionTopologyConfig.Empty;
        }

        if (topologyInputs.Length > 1)
        {
            foreach (var topologyInput in topologyInputs.Skip(1))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Only one SolutionTopology section may be configured after includes are resolved.", topologyInput.Element, topologyInput.Path);
            }
        }

        var root = topologyInputs[0];
        if (!TryReadBooleanAttribute(root.Element, "requireRecognizedProjects", out var requireRecognizedProjects))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "SolutionTopology contains an invalid requireRecognizedProjects value. Use true, false, 1, or 0.", root.Element, root.Path);
            return SolutionTopologyConfig.Empty;
        }

        if (!TryReadBooleanAttribute(root.Element, "enforceAcyclic", out var enforceAcyclic))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "SolutionTopology contains an invalid enforceAcyclic value. Use true, false, 1, or 0.", root.Element, root.Path);
            return SolutionTopologyConfig.Empty;
        }

        var modules = ParseSolutionModules(root.Element, root.Path, issues);
        var rules = ParseSolutionModuleReferenceRules(root.Element, root.Path, modules, issues);
        var result = new SolutionTopologyConfig(modules, rules, requireRecognizedProjects, enforceAcyclic);

        return result;
    }

    private static ImmutableArray<SolutionModule> ParseSolutionModules(
        XElement root,
        string xmlPath,
        ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var result = ImmutableArray.CreateBuilder<SolutionModule>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in root.Elements("Module"))
        {
            var name = element.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "SolutionTopology Module requires a name.", element, xmlPath);
                continue;
            }

            var normalizedName = name!;
            if (string.Equals(normalizedName, "*", StringComparison.Ordinal) || !seenNames.Add(normalizedName))
            {
                var message = string.Equals(normalizedName, "*", StringComparison.Ordinal)
                    ? "SolutionTopology Module name '*' is reserved and may not be used."
                    : $"SolutionTopology Module '{normalizedName}' is declared more than once.";
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, message, element, xmlPath);
                continue;
            }

            var matchers = ImmutableArray.CreateBuilder<ProjectMatcher>();
            foreach (var child in element.Elements())
            {
                if (child.Name.LocalName != "Project")
                {
                    AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "SolutionTopology Module supports only Project matcher children.", child, xmlPath);
                    continue;
                }

                if (TryReadProjectMatcher(child, out var matcher))
                {
                    matchers.Add(matcher);
                    continue;
                }

                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"SolutionTopology Module '{normalizedName}' Project requires at least one matcher attribute.", child, xmlPath);
            }

            if (matchers.Count == 0)
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"SolutionTopology Module '{normalizedName}' does not contain a Project matcher.", element, xmlPath);
                continue;
            }

            var lineInfo = (IXmlLineInfo)element;
            result.Add(new SolutionModule(normalizedName, matchers.ToImmutable(), element.Attribute("description")?.Value, xmlPath, lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0, lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
        }

        var finalResult = result.ToImmutable();

        return finalResult;
    }

    private static ImmutableArray<SolutionModuleReferenceRule> ParseSolutionModuleReferenceRules(
        XElement root,
        string xmlPath,
        ImmutableArray<SolutionModule> modules,
        ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var moduleNames = new HashSet<string>(modules.Select(module => module.Name), StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<SolutionModuleReferenceRule>();
        foreach (var element in root.Elements().Where(element => element.Name.LocalName is "AllowedModuleReference" or "BlockedModuleReference"))
        {
            var from = element.Attribute("from")?.Value;
            var to = element.Attribute("to")?.Value;
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} requires from and to attributes.", element, xmlPath);
                continue;
            }

            if (element.Elements().Any())
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} does not support child elements.", element, xmlPath);
                continue;
            }

            var normalizedFrom = from!;
            var normalizedTo = to!;
            if (normalizedFrom != "*" && !moduleNames.Contains(normalizedFrom))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} references unknown source solution module '{normalizedFrom}'.", element, xmlPath);
                continue;
            }

            if (normalizedTo != "*" && !moduleNames.Contains(normalizedTo))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} references unknown target solution module '{normalizedTo}'.", element, xmlPath);
                continue;
            }

            var lineInfo = (IXmlLineInfo)element;
            var kind = element.Name.LocalName == "BlockedModuleReference" ? SolutionModuleReferenceRuleKind.Blocked : SolutionModuleReferenceRuleKind.Allowed;
            result.Add(new SolutionModuleReferenceRule(kind, normalizedFrom, normalizedTo, element.Attribute("description")?.Value, xmlPath, lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0, lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
        }

        var finalResult = result.ToImmutable();

        return finalResult;
    }
}