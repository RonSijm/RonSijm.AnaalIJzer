using System.Collections.Immutable;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Observations;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
    private static GeneratedCodeAnalysisScope ParseGeneratedCodeScope(ImmutableArray<ArchitectureConfigurationDocumentInput> documents, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var configuredScopes = documents
            .SelectMany(document => document.Root.Elements("GeneratedCode").Select(element => (Element: element, document.Path)))
            .ToArray();
        if (configuredScopes.Length == 0)
        {
            return GeneratedCodeAnalysisScope.Exclude;
        }

        if (configuredScopes.Length > 1)
        {
            foreach (var configuredScope in configuredScopes.Skip(1))
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ArchitecturalLevels may declare only one GeneratedCode scope across its included configuration files.", configuredScope.Element, configuredScope.Path);
            }

            return GeneratedCodeAnalysisScope.Exclude;
        }

        var (element, path) = configuredScopes[0];
        if (HasUnexpectedAttributes(element, "mode", "maximumDocumentLength", "description"))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode supports mode, maximumDocumentLength, and description attributes.", element, path);
            return GeneratedCodeAnalysisScope.Exclude;
        }

        if (!GeneratedCodeAnalysisModeParser.TryParse(element.Attribute("mode")?.Value, out var mode))
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode mode must be Exclude, IncludeConfigured, or IncludeAll.", element, path);
            return GeneratedCodeAnalysisScope.Exclude;
        }

        if (!TryReadGeneratedCodeMaximumDocumentLength(element, path, issues, out var maximumDocumentLength))
        {
            return GeneratedCodeAnalysisScope.Exclude;
        }

        var paths = ParseGeneratedCodePathRules(element, path, issues);
        if (mode == GeneratedCodeAnalysisMode.IncludeConfigured && paths.IsDefaultOrEmpty)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode mode IncludeConfigured requires at least one valid Path matcher.", element, path);
            return GeneratedCodeAnalysisScope.Exclude;
        }

        if (mode != GeneratedCodeAnalysisMode.IncludeConfigured && !paths.IsDefaultOrEmpty)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"GeneratedCode mode {mode} may not declare Path matchers.", element, path);
            return GeneratedCodeAnalysisScope.Exclude;
        }

        var result = new GeneratedCodeAnalysisScope(mode, paths, maximumDocumentLength);

        return result;
    }

    private static ImmutableArray<GeneratedCodePathRule> ParseGeneratedCodePathRules(XElement scope, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var rules = ImmutableArray.CreateBuilder<GeneratedCodePathRule>();
        foreach (var child in scope.Elements())
        {
            if (child.Name.LocalName != "Path")
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode supports only Path children.", child, xmlPath);
                continue;
            }

            if (child.Elements().Any())
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode Path does not support child elements.", child, xmlPath);
                continue;
            }

            var hasUnsupportedAttribute = child.Attributes().Any(attribute => attribute.Name.LocalName != "description"
                && !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage));
            if (hasUnsupportedAttribute)
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode Path contains an unsupported matcher attribute.", child, xmlPath);
                continue;
            }

            var conditions = MatcherAttributeCatalog.CreateConditions(attributeName => child.Attribute(attributeName)?.Value, MatcherAttributeProfile.ProjectOrPackage);
            if (conditions.IsDefaultOrEmpty)
            {
                AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "GeneratedCode Path requires at least one matcher attribute.", child, xmlPath);
                continue;
            }

            rules.Add(new GeneratedCodePathRule(conditions));
        }

        var result = rules.ToImmutable();

        return result;
    }

    private static bool TryReadGeneratedCodeMaximumDocumentLength(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out int maximumDocumentLength)
    {
        var text = element.Attribute("maximumDocumentLength")?.Value;
        if (text is null)
        {
            maximumDocumentLength = GeneratedCodeAnalysisScope.DefaultMaximumDocumentLength;
            return true;
        }

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out maximumDocumentLength)
            || maximumDocumentLength < 1
            || maximumDocumentLength > GeneratedCodeAnalysisScope.MaximumSupportedDocumentLength)
        {
            AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"GeneratedCode maximumDocumentLength must be an integer from 1 through {GeneratedCodeAnalysisScope.MaximumSupportedDocumentLength}.", element, xmlPath);
            maximumDocumentLength = 0;
            return false;
        }

        return true;
    }
}