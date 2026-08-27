using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ArchitectureExceptionPolicy ParseExceptionPolicy(IEnumerable<ArchitectureConfigurationDocumentInput> documents, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policyElements = documents
			.SelectMany(document => document.Root.Elements("ExceptionPolicy").Select(element => (Element: element, document.Path)))
			.ToArray();
		if (policyElements.Length == 0)
		{
			return ArchitectureExceptionPolicy.Disabled;
		}

		var first = policyElements[0];
		for (var index = 1; index < policyElements.Length; index++)
		{
			if (!ExceptionPolicyAttributesEqual(first.Element, policyElements[index].Element))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Only one effective ExceptionPolicy may be configured across the combined architecture settings.", policyElements[index].Element, policyElements[index].Path);
			}
		}

		var requireReason = ParseExceptionPolicyBoolean(first.Element, first.Path, "requireReason", issues, false);
		var requireOwner = ParseExceptionPolicyBoolean(first.Element, first.Path, "requireOwner", issues, false);
		var requireExpiresOn = ParseExceptionPolicyBoolean(first.Element, first.Path, "requireExpiresOn", issues, false);
		var warnBeforeDays = ParseExceptionPolicyInteger(first.Element, first.Path, "warnBeforeDays", 14, 0, 3650, issues);
		var lineInfo = (IXmlLineInfo)first.Element;
		var result = new ArchitectureExceptionPolicy(
			true,
			requireReason,
			requireOwner,
			requireExpiresOn,
			warnBeforeDays,
			first.Element.Attribute("description")?.Value,
			first.Path,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);

		return result;
	}

	private static ImmutableArray<ExceptionMatcher> ParseExceptions(
		XElement ruleEl,
		string xmlPath,
		string ownerLayerPath,
		ArchitectureExceptionPolicy policy,
		ImmutableArray<ArchitectureExceptionDefinition>.Builder definitions,
		ImmutableArray<ArchitectureExceptionReview>.Builder reviews)
	{
		var exceptionsContainer = ruleEl.Element("Exceptions");
		if (exceptionsContainer is null)
		{
			return ImmutableArray<ExceptionMatcher>.Empty;
		}

		var builder = ImmutableArray.CreateBuilder<ExceptionMatcher>();

		foreach (var exEl in exceptionsContainer.Elements().Where(element => element.Name.LocalName is "Class" or "Namespace" or "Assembly"))
		{
			var target = exEl.Name.LocalName switch
			{
				"Namespace" => MatchTarget.Namespace,
				"Assembly" => MatchTarget.Assembly,
				_ => MatchTarget.TypeName
			};
			if (!ArchitectureConfigurationMatcherReader.TryReadMatcher(exEl, target, out var matcher))
			{
				continue;
			}

			builder.Add(ParseExceptionMatcher(exEl, matcher, xmlPath, ownerLayerPath, policy, definitions, reviews));
		}

		var result = builder.ToImmutable();

		return result;
	}

	private static ExceptionMatcher ParseExceptionMatcher(
		XElement element,
		PatternMatcher matcher,
		string xmlPath,
		string ownerLayerPath,
		ArchitectureExceptionPolicy policy,
		ImmutableArray<ArchitectureExceptionDefinition>.Builder definitions,
		ImmutableArray<ArchitectureExceptionReview>.Builder reviews)
	{
		var nestedMatchers = ParseExceptions(element, xmlPath, ownerLayerPath, policy, definitions, reviews);
		var nestedDefinitions = nestedMatchers.Select(exception => exception.Definition).ToImmutableArray();
		var metadata = ParseExceptionMetadata(element);
		var matcherKind = element.Name.LocalName;
		var matcherLabel = ArchitectureConfigurationDocumentationBuilder.GetMatcherDisplayName(element) ?? "(no matcher)";
		var lineInfo = (IXmlLineInfo)element;
		var review = ArchitectureExceptionEvaluator.Evaluate(
			policy,
			matcherKind,
			matcherLabel,
			metadata,
			ownerLayerPath,
			xmlPath,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
		if (review is { } exceptionReview)
		{
			reviews.Add(exceptionReview);
		}

		var definition = new ArchitectureExceptionDefinition(
			matcherKind,
			matcherLabel,
			matcher,
			metadata,
			nestedDefinitions,
			ownerLayerPath,
			xmlPath,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0,
			review?.Status ?? ArchitectureExceptionStatus.Active);
		definitions.Add(definition);

		var result = new ExceptionMatcher(definition, nestedMatchers);

		return result;
	}
}

