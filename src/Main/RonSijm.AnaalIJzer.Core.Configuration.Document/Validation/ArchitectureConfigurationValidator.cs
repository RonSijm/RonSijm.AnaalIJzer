using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Symbols;
using RonSijm.AnaalIJzer.Core.Matchers.Declarations;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Validation;

public static class ArchitectureConfigurationValidator
{
	public static ImmutableArray<ConfigurationIssue> Validate(XDocument document, string configPath)
	{
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

		var settings = new XmlReaderSettings
		{
			Schemas = ArchitectureConfigurationSchemaProvider.Schemas,
			ValidationType = ValidationType.Schema
		};
		settings.ValidationEventHandler += (_, args) =>
		{
			var exception = args.Exception;
			issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Architecture XML schema validation failed: {args.Message}", configPath, exception?.LineNumber ?? 0, exception?.LinePosition ?? 0));
		};

		using (var reader = document.CreateReader())
		using (var validatingReader = XmlReader.Create(reader, settings))
		{
			while (validatingReader.Read())
			{
			}
		}

		foreach (var element in document.Descendants().Where(element => IsMatcherElementName(element.Name.LocalName)))
		{
			ValidateMatcherElement(element, configPath, issues);
		}

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "Project"))
		{
			ValidateProjectMatcherElement(element, configPath, issues);
		}

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "Package"))
		{
			ValidatePackageMatcherElement(element, configPath, issues);
		}

		var result = issues.ToImmutable();

		return result;
	}

	private static void ValidateMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		if (IsCodeObservationMatcherElementName(element.Name.LocalName))
		{
			ValidateCodeObservationMatcherElement(element, configPath, issues);

			return;
		}

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

	private static void ValidateCodeObservationMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var unsupportedAttributes = element.Attributes()
			.Where(attribute => IsMatcherAttribute(attribute.Name.LocalName) && !IsCodeObservationMatcherAttribute(attribute.Name.LocalName))
			.Select(attribute => attribute.Name.LocalName)
			.ToArray();
		if (unsupportedAttributes.Length > 0)
		{
			AddIssue(
				issues,
				ConfigurationIssueKind.InvalidConfiguration,
				$"{element.Name.LocalName} supports typeName, exactName, exactFullName, endsWith, startsWith, contains, or regex matchers.",
				element,
				configPath);
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

	private static bool IsMatcherElementName(string name)
	{
		var result = name is "Class" or "Namespace" or "Assembly" or "Name" or "Source" or "Target"
		             || IsDeclarationMatcherElementName(name)
		             || IsCodeObservationMatcherElementName(name);

		return result;
	}

	private static bool IsDeclarationMatcherElementName(string name)
	{
		var result = DeclarationMatchTargetParser.TryParse(name, out _);

		return result;
	}

	private static bool IsCodeObservationMatcherElementName(string name)
	{
		var result = CodeObservationMatchTargetParser.TryParse(name, out _);

		return result;
	}

	private static bool IsCodeObservationMatcherAttribute(string name)
	{
		var result = name is "typeName" or "exactName" or "exactFullName" or "endsWith" or "startsWith" or "contains" or "regex";

		return result;
	}

	private static void ValidateProjectMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var configuredMatchers = element.Attributes().Where(attribute => attribute.Name.LocalName is "typeName" or "exactName" or "startsWith" or "endsWith" or "contains" or "regex").ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Project requires at least one matcher attribute.", element, configPath);

			return;
		}

		if (element.Attributes().Any(attribute => IsMatcherAttribute(attribute.Name.LocalName) && attribute.Name.LocalName is "exactFullName" or "inherits" or "implements" or "withAttribute" or "withAccessModifier" or "typeKind"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Project supports typeName, exactName, startsWith, endsWith, contains, or regex matchers.", element, configPath);
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

	private static void ValidatePackageMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var configuredMatchers = element.Attributes().Where(attribute => attribute.Name.LocalName is "typeName" or "exactName" or "startsWith" or "endsWith" or "contains" or "regex").ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Package requires at least one matcher attribute.", element, configPath);

			return;
		}

		if (element.Attributes().Any(attribute => IsMatcherAttribute(attribute.Name.LocalName) && attribute.Name.LocalName is "exactFullName" or "inherits" or "implements" or "withAttribute" or "withAccessModifier" or "typeKind"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Package supports typeName, exactName, startsWith, endsWith, contains, or regex matchers.", element, configPath);
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

	private static void AddIssue(ImmutableArray<ConfigurationIssue>.Builder issues, ConfigurationIssueKind kind, string message, XElement element, string path)
	{
		var line = (IXmlLineInfo)element;
		issues.Add(new ConfigurationIssue(kind, message, path, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
	}
}
