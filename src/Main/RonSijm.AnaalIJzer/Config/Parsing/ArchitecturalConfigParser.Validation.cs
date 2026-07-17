using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.Symbols;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Parsing;

internal static partial class ArchitecturalConfigParser
{
	private static AnalyzerConfig CreateInvalidConfig(ImmutableArray<ConfigurationIssue> issues)
	{
		var config = AnalyzerConfig.Invalid(issues[0]);
		if (issues.Length == 1)
		{
			return config;
		}

		return new AnalyzerConfig(new LayerRegistry(ImmutableArray<LayerNode>.Empty, ImmutableDictionary<string, LayerNode>.Empty, ImmutableDictionary<string, MatcherRule>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty), new DependencyGraph(ImmutableArray<DependencyEdge>.Empty), new OutputConfig(false, string.Empty, false, string.Empty), ImmutableHashSet<string>.Empty, ImmutableDictionary<string, ImmutableHashSet<string>>.Empty, false, ImmutableArray<LayerNode>.Empty, ImmutableArray<string>.Empty, ImmutableArray<(string, string?)>.Empty, ArchitectureDocumentation.Empty, issues);
	}

	private static void ValidateDocument(XDocument document, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		document.Validate(ConfigurationSchemas.Value, (_, args) =>
		{
			var exception = args.Exception;
			issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Architecture XML schema validation failed: {args.Message}", configPath, exception?.LineNumber ?? 0, exception?.LinePosition ?? 0));
		}, true);

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName is "Class" or "Namespace" or "Assembly"))
		{
			ValidateMatcherElement(element, configPath, issues);
		}
	}

	private static void ValidateMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var configuredMatchers = element.Attributes().Where(attribute => IsMatcherAttribute(attribute.Name.LocalName)).ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} requires at least one matcher attribute.", element, configPath);
			return;
		}

		if (element.Name.LocalName is "Namespace" or "Assembly"
		    && configuredMatchers.Any(attribute => attribute.Name.LocalName is "typeName" or "exactFullName" or "inherits" or "implements" or "withAttribute" or "withAccessModifier" or "typeKind"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} supports exactName, endsWith, startsWith, contains, or regex matchers.", element, configPath);
		}

		if (element.Attribute("typeKind")?.Value is { } typeKind && !ITypeSymbolTypeKindExtension.IsSupportedTypeKind(typeKind))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Unknown typeKind '{typeKind}'. Supported values: Class, Interface, Struct, Record, RecordStruct, Enum, Delegate.", element, configPath);
		}

		var regex = element.Attribute("regex")?.Value;
		if (regex is null)
		{
			return;
		}

		try
		{
			_ = new Regex(regex, RegexOptions.CultureInvariant);
		}
		catch (ArgumentException ex)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Invalid regular expression '{regex}': {ex.Message}", element, configPath);
		}
	}

	private static bool IsMatcherAttribute(string name)
	{
		var result = name is
			"typeName" or "exactName" or "exactFullName" or "inherits" or "implements" or "withAttribute" or
			"withAccessModifier" or "typeKind" or "endsWith" or "startsWith" or "contains" or "regex";

		return result;
	}

	private static void AddIssue(ImmutableArray<ConfigurationIssue>.Builder issues, ConfigurationIssueKind kind, string message, XElement element, string path)
	{
		var line = (IXmlLineInfo)element;
		issues.Add(new ConfigurationIssue(kind, message, path, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
	}

	private static XmlSchemaSet CreateConfigurationSchemas()
	{
		var assembly = typeof(ArchitecturalConfigParser).GetTypeInfo().Assembly;
		using var stream = assembly.GetManifestResourceStream("RonSijm.AnaalIJzer.AnaalIJzer.xsd")
			?? throw new InvalidOperationException("Embedded AnaalIJzer.xsd schema was not found.");
		using var reader = XmlReader.Create(stream);
		var schemas = new XmlSchemaSet();
		schemas.Add(string.Empty, reader);
		schemas.Compile();
		return schemas;
	}
}
