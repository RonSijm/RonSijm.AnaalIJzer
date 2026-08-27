using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.GraphModel.Loading;

public static partial class ArchitectureGraphXmlSnapshotLoader
{
	private const string LayerElementName = "Layer";
	private const string AllowedElementName = "Allowed";
	private const string ForbiddenElementName = "Forbidden";
	private const string ExceptionPolicyElementName = "ExceptionPolicy";
	private const string ExceptionsElementName = "Exceptions";
	private const string RequireExceptionReasonAttributeName = "requireReason";
	private const string RequireExceptionOwnerAttributeName = "requireOwner";
	private const string RequireExceptionExpiresOnAttributeName = "requireExpiresOn";
	private const string ExceptionWarnBeforeDaysAttributeName = "warnBeforeDays";

	public static ArchitectureGraphSnapshot Load(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Choose an Architecture.anl file first.", nameof(path));
		}

		var fullPath = Path.GetFullPath(path);
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, fullPath);
		var result = Load(source);

		return result;
	}

	public static ArchitectureGraphSnapshot Load(ArchitectureConfigurationSource source)
	{
		if (!source.CanEdit)
		{
			throw new InvalidOperationException("Choose an editable AnaalIJzer configuration source first.");
		}

		var fullPath = Path.GetFullPath(source.Path);
		var normalizedSource = new ArchitectureConfigurationSource(source.Kind, fullPath);
		if (!ArchitectureConfigurationDocumentLoader.TryReadConfigurationDocument(normalizedSource, out var document, out var message) || document is null)
		{
			throw new InvalidOperationException(message);
		}

		if (document.Root is null || !IsElement(document.Root, "ArchitecturalLevels"))
		{
			throw new InvalidOperationException("The selected AnaalIJzer configuration does not have an <ArchitecturalLevels> root element.");
		}

		var documents = ImmutableArray.CreateBuilder<ConfigurationDocumentPart>();
		CollectConfigurationDocuments(document.Root, fullPath, normalizedSource.Kind, documents, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		var configurationDocuments = documents.ToImmutable();
		var layers = ImmutableArray.CreateBuilder<ArchitectureGraphLayer>();
		foreach (var configurationDocument in configurationDocuments)
		{
			CollectLayers(configurationDocument.Root, string.Empty, configurationDocument.SourcePath, configurationDocument.SourceKind, layers);
		}

		var layerPaths = layers.Select(layer => layer.Path).ToImmutableHashSet(StringComparer.Ordinal);
		var rules = ImmutableArray.CreateBuilder<ArchitectureGraphRule>();
		foreach (var configurationDocument in configurationDocuments)
		{
			CollectRules(configurationDocument.Root, string.Empty, configurationDocument.SourcePath, configurationDocument.SourceKind, layerPaths, rules);
		}
		var exceptionReviews = ImmutableArray.CreateBuilder<ArchitectureGraphExceptionReview>();
		var exceptionPolicy = ReadExceptionPolicy(configurationDocuments);
		foreach (var configurationDocument in configurationDocuments)
		{
			CollectExceptionReviews(configurationDocument.Root, string.Empty, configurationDocument.SourcePath, exceptionPolicy, exceptionReviews);
		}

		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			layers.ToImmutable(),
			rules.ToImmutable(),
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty,
			normalizedSource,
			exceptionReviews: exceptionReviews.ToImmutable());

		return result;
	}

	private static bool IsDependencyElement(XElement element)
	{
		var result = IsElement(element, "AllowedDependency") || IsElement(element, "BlockedDependency");

		return result;
	}

	private static bool IsElement(XElement element, string name)
	{
		var result = string.Equals(element.Name.LocalName, name, StringComparison.Ordinal);

		return result;
	}

	private static bool IsMatcherElement(XElement element)
	{
		var result = element.Name.LocalName is "Class" or "Namespace" or "Assembly";

		return result;
	}

	private static bool IsPolicyMatcherElement(XElement element)
	{
		var result = element.Name.LocalName is "Class" or "Namespace";

		return result;
	}
}
