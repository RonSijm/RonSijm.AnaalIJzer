using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Loading;

public static partial class ArchitectureGraphXmlSnapshotLoader
{
	private static void CollectExceptionReviews(
		XElement container,
		string scopePath,
		string sourcePath,
		LocalExceptionPolicy policy,
		ImmutableArray<ArchitectureGraphExceptionReview>.Builder reviews)
	{
		foreach (var matcher in container.Elements().Where(IsMatcherElement))
		{
			CollectExceptionReviewsForOwner(matcher, scopePath, sourcePath, policy, reviews);
		}

		foreach (var matcher in container.Elements(AllowedElementName).SelectMany(item => item.Elements().Where(IsPolicyMatcherElement)))
		{
			CollectExceptionReviewsForOwner(matcher, scopePath, sourcePath, policy, reviews);
		}

		foreach (var matcher in container.Elements(ForbiddenElementName).SelectMany(item => item.Elements().Where(IsPolicyMatcherElement)))
		{
			CollectExceptionReviewsForOwner(matcher, scopePath, sourcePath, policy, reviews);
		}

		foreach (var layer in container.Elements().Where(element => IsElement(element, LayerElementName)))
		{
			var name = layer.Attribute("name")?.Value?.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var nextScope = string.IsNullOrWhiteSpace(scopePath) ? name! : scopePath + "/" + name!;
			CollectExceptionReviews(layer, nextScope, sourcePath, policy, reviews);
		}
	}

	private static void CollectExceptionReviewsForOwner(
		XElement ownerElement,
		string ownerLayerPath,
		string sourcePath,
		LocalExceptionPolicy policy,
		ImmutableArray<ArchitectureGraphExceptionReview>.Builder reviews)
	{
		var exceptions = ownerElement.Element(ExceptionsElementName);
		if (exceptions is null)
		{
			return;
		}

		foreach (var matcher in exceptions.Elements().Where(IsMatcherElement))
		{
			AddExceptionReview(matcher, ownerLayerPath, sourcePath, policy, reviews);
		}
	}

	private static void AddExceptionReview(
		XElement matcher,
		string ownerLayerPath,
		string sourcePath,
		LocalExceptionPolicy policy,
		ImmutableArray<ArchitectureGraphExceptionReview>.Builder reviews)
	{
		var status = EvaluateExceptionStatus(matcher, policy);
		if (status is null)
		{
			var nested = matcher.Element(ExceptionsElementName);
			if (nested is null)
			{
				return;
			}

			foreach (var nestedMatcher in nested.Elements().Where(IsMatcherElement))
			{
				AddExceptionReview(nestedMatcher, ownerLayerPath, sourcePath, policy, reviews);
			}

			return;
		}

		var line = (IXmlLineInfo)matcher;
		var expiresOn = matcher.Attribute("expiresOn")?.Value?.Trim();
		reviews.Add(new ArchitectureGraphExceptionReview(
			ownerLayerPath,
			matcher.Name.LocalName,
			FormatMatcherLabel(matcher),
			status.Value.Status,
			status.Value.Message,
			matcher.Attribute("reason")?.Value?.Trim(),
			matcher.Attribute("owner")?.Value?.Trim(),
			string.IsNullOrWhiteSpace(expiresOn) ? null : expiresOn,
			sourcePath,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0));

		var childExceptions = matcher.Element(ExceptionsElementName);
		if (childExceptions is null)
		{
			return;
		}

		foreach (var nestedMatcher in childExceptions.Elements().Where(IsMatcherElement))
		{
			AddExceptionReview(nestedMatcher, ownerLayerPath, sourcePath, policy, reviews);
		}
	}

}
