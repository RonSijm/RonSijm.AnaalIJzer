using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
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

		foreach (var element in document.Descendants().Where(IsMatcherElement))
		{
			ValidateMatcherElement(element, configPath, issues);
		}

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "Project"))
		{
			ValidateProjectMatcherElement(element, configPath, issues);
		}

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "Package"))
		{
			ValidateReferenceIdentityMatcherElement(element, configPath, issues);
		}

		foreach (var element in document.Descendants().Where(IsAssemblyAttributeArgumentElement))
		{
			ValidateAssemblyAttributeArgumentElement(element, configPath, issues);
		}

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "Assembly" && element.Parent?.Parent?.Name.LocalName == "AssemblyReferencePolicy"))
		{
			ValidateReferenceIdentityMatcherElement(element, configPath, issues);
		}

		var result = issues.ToImmutable();

		return result;
	}

	private static void ValidateMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		if (IsReturnValueMatcherRule(element))
		{
			ValidateReturnValueMatcherRule(element, configPath, issues);

			return;
		}

		if (IsCodeObservationMatcherElementName(element.Name.LocalName))
		{
			ValidateCodeObservationMatcherElement(element, configPath, issues);

			return;
		}

		var configuredMatchers = element.Attributes().Where(attribute => MatcherAttributeCatalog.IsMatcherAttribute(attribute.Name.LocalName)).ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} requires at least one matcher attribute.", element, configPath);

			return;
		}

		var profile = element.Name.LocalName is "Namespace" or "Assembly"
			? MatcherAttributeProfile.NamespaceOrAssembly
			: MatcherAttributeProfile.Type;
		if (configuredMatchers.Any(attribute => !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, profile)))
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

	private static bool IsReturnValueMatcherRule(XElement element)
	{
		var result = element.Parent?.Name.LocalName is "ReturnValuePolicy" or "AllowedReturn"
		             && CodeObservationMatchTargetParser.TryParse(element.Name.LocalName, out _);

		return result;
	}

	private static void ValidateReturnValueMatcherRule(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		if (!CodeObservationMatchTargetParser.TryParse(element.Name.LocalName, out var target)
			|| target == CodeObservationMatchTarget.Throw)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ReturnValuePolicy and AllowedReturn support Literal, Invocation, New, Identifier, and MemberAccess matcher children.", element, configPath);

			return;
		}

		var unsupportedAttributes = element.Attributes()
			.Where(attribute => attribute.Name.LocalName is not "description" and not "comment"
				&& !MatcherAttributeCatalog.IsSupportedAttribute(
					attribute.Name.LocalName,
					MatcherAttributeProfile.SemanticCodeObservation,
					target == CodeObservationMatchTarget.Literal))
			.Select(attribute => attribute.Name.LocalName)
			.ToArray();
		if (unsupportedAttributes.Length > 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"ReturnValuePolicy {element.Name.LocalName} supports standard matcher attributes{(target == CodeObservationMatchTarget.Literal ? " and value" : string.Empty)}.", element, configPath);
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
			.Where(attribute => MatcherAttributeCatalog.IsMatcherAttribute(attribute.Name.LocalName)
				&& !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.CodeObservation))
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

	private static bool IsMatcherElement(XElement element)
	{
		var name = element.Name.LocalName;
		var result = !IsAssemblyReferenceMatcherElement(element)
		             && (name is "Class" or "Namespace" or "Assembly" or "Name" or "Source" or "Target"
		                 || IsAssemblyAttributeRuleElement(element)
		                 || IsDeclarationMatcherElementName(name)
		                 || IsCodeObservationMatcherElementName(name));

		return result;
	}

	private static bool IsAssemblyAttributeRuleElement(XElement element)
	{
		var result = element.Name.LocalName == "Attribute"
		             && element.Parent?.Name.LocalName is "Allowed" or "Forbidden"
		             && element.Parent.Parent?.Name.LocalName == "AssemblyAttributePolicy";

		return result;
	}

	private static bool IsAssemblyAttributeArgumentElement(XElement element)
	{
		var result = element.Name.LocalName == "Argument"
			             && element.Parent?.Name.LocalName == "Attribute"
			             && element.Parent.Parent?.Parent?.Name.LocalName == "AssemblyAttributePolicy";

		return result;
	}

	private static void ValidateAssemblyAttributeArgumentElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var index = element.Attribute("index")?.Value;
		var name = element.Attribute("name")?.Value;
		if (string.IsNullOrWhiteSpace(index) == string.IsNullOrWhiteSpace(name))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument requires exactly one of index or name.", element, configPath);
		}

		var configuredMatchers = element.Attributes()
			.Where(attribute => MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage))
			.ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument requires at least one textual matcher attribute.", element, configPath);
		}

		if (element.Attributes().Any(attribute => MatcherAttributeCatalog.IsMatcherAttribute(attribute.Name.LocalName)
			&& !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage)))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument supports typeName, exactName, startsWith, endsWith, contains, or regex matchers.", element, configPath);
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
		catch (ArgumentException exception)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Invalid regular expression '{regex}': {exception.Message}", element, configPath);
		}
	}

	private static bool IsAssemblyReferenceMatcherElement(XElement element)
	{
		var result = element.Name.LocalName == "Assembly" && element.Parent?.Parent?.Name.LocalName == "AssemblyReferencePolicy";

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

	private static void ValidateProjectMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var configuredMatchers = element.Attributes()
			.Where(attribute => MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage))
			.ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Project requires at least one matcher attribute.", element, configPath);

			return;
		}

		if (element.Attributes().Any(attribute => MatcherAttributeCatalog.IsMatcherAttribute(attribute.Name.LocalName)
			&& !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage)))
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

	private static void ValidateReferenceIdentityMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var configuredMatchers = element.Attributes()
			.Where(attribute => MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage))
			.ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} requires at least one matcher attribute.", element, configPath);

			return;
		}

		if (element.Attributes().Any(attribute => MatcherAttributeCatalog.IsMatcherAttribute(attribute.Name.LocalName)
			&& !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage)))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} supports typeName, exactName, startsWith, endsWith, contains, or regex matchers.", element, configPath);
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
