using System.Collections.Immutable;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class ConfigurationCodeFixSupport
{
	private static readonly string[] AccessibilityOrder =
	[
		"Public",
		"Internal",
		"Protected",
		"ProtectedInternal",
		"PrivateProtected",
		"Private",
		"File",
	];

	internal static async Task<ArchitectureConfigurationSource> FindDefaultConfigurationSourceAsync(Document document, CancellationToken cancellationToken)
	{
		var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
		if (compilation is null)
		{
			return ArchitectureConfigurationSource.None;
		}

		var result = ArchitectureConfigurationSourceDiscovery.FindConfigurationSource(
			document.FilePath,
			document.Project.AnalyzerOptions.AdditionalFiles,
			compilation,
			cancellationToken);

		return result;
	}

	internal static async Task<ImmutableArray<ConfigurationDocumentSnapshot>> GetConfigurationSnapshotsAsync(Document document, CancellationToken cancellationToken)
	{
		var builder = ImmutableArray.CreateBuilder<ConfigurationDocumentSnapshot>();
		var seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var additionalDocument in document.Project.AdditionalDocuments)
		{
			if (additionalDocument.FilePath is not { Length: > 0 } filePath
			    || !filePath.EndsWith(".anl", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, filePath);
			if (!seenSources.Add(BuildSourceKey(source)))
			{
				continue;
			}

			var text = await additionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
			if (TryParse(text.ToString(), out var parsed))
			{
				builder.Add(new ConfigurationDocumentSnapshot(source, parsed));
			}
		}

		var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
		if (compilation is not null)
		{
			var inlineConfiguration = ArchitectureConfigurationSourceDiscovery.TryReadInlineConfigurationTextDocument(
				compilation,
				document.FilePath,
				cancellationToken);
			if (inlineConfiguration is not null)
			{
				var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, inlineConfiguration.Path);
				if (seenSources.Add(BuildSourceKey(source)) && TryParse(inlineConfiguration.Content, out var parsed))
				{
					builder.Add(new ConfigurationDocumentSnapshot(source, parsed));
				}
			}
		}

		var result = builder.ToImmutable();

		return result;
	}

	internal static ArchitectureConfigurationSource ResolveSource(
		ArchitectureConfigurationSource discoveredSource,
		string? sourcePath,
		ImmutableArray<ConfigurationDocumentSnapshot> snapshots)
	{
		if (!string.IsNullOrWhiteSpace(sourcePath))
		{
			var normalizedSourcePath = ArchitectureConfigurationSourceLookup.NormalizePath(sourcePath!);
			foreach (var snapshot in snapshots)
			{
				if (string.Equals(
					    ArchitectureConfigurationSourceLookup.NormalizePath(snapshot.Source.Path),
					    normalizedSourcePath,
					    StringComparison.OrdinalIgnoreCase))
				{
					return snapshot.Source;
				}
			}

			var resolvedSource = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, sourcePath!);

			return resolvedSource;
		}

		var result = discoveredSource;

		return result;
	}

	internal static int ReadIntProperty(Diagnostic diagnostic, string propertyName)
	{
		if (!diagnostic.Properties.TryGetValue(propertyName, out var value)
		    || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
		{
			return 0;
		}

		var result = parsed;

		return result;
	}

	internal static string ReadStringProperty(Diagnostic diagnostic, string propertyName)
	{
		var result = diagnostic.Properties.TryGetValue(propertyName, out var value)
			? value ?? string.Empty
			: string.Empty;

		return result;
	}

	internal static XElement? FindElementByLineInfo(XDocument document, string localName, int xmlLineNumber, int xmlLinePosition)
	{
		var result = FindElementByLineInfo(document.Descendants(localName), xmlLineNumber, xmlLinePosition);

		return result;
	}

	internal static XElement? FindElementByLineInfo(IEnumerable<XElement> elements, int xmlLineNumber, int xmlLinePosition)
	{
		if (xmlLineNumber <= 0)
		{
			return null;
		}

		foreach (var element in elements)
		{
			var lineInfo = (IXmlLineInfo)element;
			if (!lineInfo.HasLineInfo()
			    || lineInfo.LineNumber != xmlLineNumber
			    || xmlLinePosition > 0 && lineInfo.LinePosition != xmlLinePosition)
			{
				continue;
			}

			return element;
		}

		return null;
	}

	internal static XElement? FindLayerElement(XDocument document, string layerPath)
	{
		if (document.Root is null || string.IsNullOrWhiteSpace(layerPath))
		{
			return null;
		}

		var current = document.Root;
		foreach (var segment in SplitLayerPath(layerPath))
		{
			current = current.Elements("Layer")
				.FirstOrDefault(element => string.Equals(element.Attribute("name")?.Value, segment, StringComparison.Ordinal));
			if (current is null)
			{
				return null;
			}
		}

		var result = current;

		return result;
	}

	internal static ImmutableArray<LayerElementSnapshot> GetLayerElements(ConfigurationDocumentSnapshot snapshot)
	{
		var builder = ImmutableArray.CreateBuilder<LayerElementSnapshot>();
		if (snapshot.Document.Root is null)
		{
			return builder.ToImmutable();
		}

		AddLayerElements(builder, snapshot, snapshot.Document.Root, string.Empty);
		var result = builder.ToImmutable();

		return result;
	}

	internal static ImmutableArray<string> GetAncestorLayerPaths(string layerPath)
	{
		var parts = SplitLayerPath(layerPath);
		var builder = ImmutableArray.CreateBuilder<string>(parts.Length);
		for (var index = 0; index < parts.Length; index++)
		{
			builder.Add(string.Join("/", parts.Take(index + 1)));
		}

		var result = builder.ToImmutable();

		return result;
	}

	internal static SortedSet<string> ReadSites(string? attributeValue)
	{
		var result = new SortedSet<string>(DependencySiteComparer.Instance);
		if (string.IsNullOrWhiteSpace(attributeValue))
		{
			return result;
		}

		foreach (var token in attributeValue!.Split(','))
		{
			var trimmed = token.Trim();
			if (DependencySites.TryNormalize(trimmed, out var normalized))
			{
				result.Add(normalized);
			}
		}

		return result;
	}

	internal static string FormatSites(IEnumerable<string> sites)
	{
		var result = string.Join(", ", sites);

		return result;
	}

	internal static SortedSet<string> ReadAccessibilities(string? attributeValue)
	{
		var result = new SortedSet<string>(AccessibilityComparer.Instance);
		if (string.IsNullOrWhiteSpace(attributeValue))
		{
			return result;
		}

		foreach (var token in attributeValue!.Split(','))
		{
			var trimmed = token.Trim();
			if (TryNormalizeAccessibility(trimmed, out var normalized))
			{
				result.Add(normalized);
			}
		}

		return result;
	}

	internal static string FormatAccessibilities(IEnumerable<string> accessibilities)
	{
		var result = string.Join(", ", accessibilities);

		return result;
	}

	internal static bool SiteFilterAllows(XElement element, string site)
	{
		var allowedSites = ReadSites(element.Attribute("allowedSites")?.Value);
		if (allowedSites.Count > 0 && !allowedSites.Contains(site))
		{
			return false;
		}

		var blockedSites = ReadSites(element.Attribute("blockedSites")?.Value);
		var result = !blockedSites.Contains(site);

		return result;
	}

	internal static bool TryNormalizeAccessibility(string value, out string normalized)
	{
		foreach (var accessibility in AccessibilityOrder)
		{
			if (string.Equals(value, accessibility, StringComparison.OrdinalIgnoreCase))
			{
				normalized = accessibility;
				return true;
			}
		}

		normalized = string.Empty;
		return false;
	}

	private static void AddLayerElements(
		ImmutableArray<LayerElementSnapshot>.Builder builder,
		ConfigurationDocumentSnapshot snapshot,
		XElement parent,
		string parentPath)
	{
		foreach (var layerElement in parent.Elements("Layer"))
		{
			var name = layerElement.Attribute("name")?.Value;
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var layerPath = string.IsNullOrWhiteSpace(parentPath)
				? name!
				: parentPath + "/" + name;
			builder.Add(new LayerElementSnapshot(snapshot.Source, snapshot.Document, layerElement, layerPath));
			AddLayerElements(builder, snapshot, layerElement, layerPath);
		}
	}

	private static string[] SplitLayerPath(string layerPath)
	{
		var result = layerPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

		return result;
	}

	private static string BuildSourceKey(ArchitectureConfigurationSource source)
	{
		var result = source.Kind + "|" + ArchitectureConfigurationSourceLookup.NormalizePath(source.Path);

		return result;
	}

	private static bool TryParse(string content, out XDocument document)
	{
		try
		{
			document = XDocument.Parse(content, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
			return true;
		}
		catch
		{
			document = null!;
			return false;
		}
	}

	internal sealed class ConfigurationDocumentSnapshot(ArchitectureConfigurationSource source, XDocument document)
	{
		public ArchitectureConfigurationSource Source { get; } = source;

		public XDocument Document { get; } = document;
	}

	internal sealed class LayerElementSnapshot(
		ArchitectureConfigurationSource source,
		XDocument document,
		XElement element,
		string layerPath)
	{
		public ArchitectureConfigurationSource Source { get; } = source;

		public XDocument Document { get; } = document;

		public XElement Element { get; } = element;

		public string LayerPath { get; } = layerPath;
	}

	private sealed class DependencySiteComparer : IComparer<string>
	{
		internal static DependencySiteComparer Instance { get; } = new();

		public int Compare(string? x, string? y)
		{
			if (ReferenceEquals(x, y))
			{
				return 0;
			}

			if (x is null)
			{
				return -1;
			}

			if (y is null)
			{
				return 1;
			}

			var xIndex = Array.IndexOf(DependencySites.All, x);
			var yIndex = Array.IndexOf(DependencySites.All, y);
			if (xIndex >= 0 && yIndex >= 0 && xIndex != yIndex)
			{
				return xIndex.CompareTo(yIndex);
			}

			var result = string.Compare(x, y, StringComparison.Ordinal);

			return result;
		}
	}

	private sealed class AccessibilityComparer : IComparer<string>
	{
		internal static AccessibilityComparer Instance { get; } = new();

		public int Compare(string? x, string? y)
		{
			if (ReferenceEquals(x, y))
			{
				return 0;
			}

			if (x is null)
			{
				return -1;
			}

			if (y is null)
			{
				return 1;
			}

			var xIndex = Array.IndexOf(AccessibilityOrder, x);
			var yIndex = Array.IndexOf(AccessibilityOrder, y);
			if (xIndex >= 0 && yIndex >= 0 && xIndex != yIndex)
			{
				return xIndex.CompareTo(yIndex);
			}

			var result = string.Compare(x, y, StringComparison.Ordinal);

			return result;
		}
	}
}
