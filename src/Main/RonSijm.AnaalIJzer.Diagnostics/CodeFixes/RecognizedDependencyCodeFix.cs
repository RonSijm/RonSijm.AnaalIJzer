using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class RecognizedDependencyCodeFix
{
	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var dependencyTypeName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDepTypeName);
		var callerLayerName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyCallerLayerName);
		var site = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertySite);
		if (string.IsNullOrWhiteSpace(dependencyTypeName) || string.IsNullOrWhiteSpace(callerLayerName))
		{
			return;
		}

		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		if (snapshots.IsDefaultOrEmpty)
		{
			return;
		}

		RegisterClassificationFixes(context, diagnostic, snapshots, dependencyTypeName);
		RegisterRequirementRelaxationFixes(context, diagnostic, snapshots, callerLayerName, site);
	}

	private static void RegisterClassificationFixes(
		CodeFixContext context,
		Diagnostic diagnostic,
		ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots,
		string dependencyTypeName)
	{
		var seenLayers = new HashSet<string>(StringComparer.Ordinal);
		foreach (var snapshot in snapshots)
		{
			foreach (var layer in ConfigurationCodeFixSupport.GetLayerElements(snapshot))
			{
				if (!seenLayers.Add(layer.LayerPath))
				{
					continue;
				}

				var title = $"Classify '{dependencyTypeName}' into layer '{layer.LayerPath}'";
				context.RegisterCodeFix(
					CodeAction.Create(
						title,
						cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
							context.Document,
							layer.Source,
							document => TryAddTypeMatcher(document, layer.LayerPath, dependencyTypeName),
							cancellationToken),
						title),
					diagnostic);
			}
		}
	}

	private static void RegisterRequirementRelaxationFixes(
		CodeFixContext context,
		Diagnostic diagnostic,
		ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots,
		string callerLayerName,
		string site)
	{
		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		foreach (var snapshot in snapshots)
		{
			var root = snapshot.Document.Root;
			if (root is null)
			{
				continue;
			}

			var requiredSites = ConfigurationCodeFixSupport.ReadSites(root.Attribute("requireRecognizedDependencies")?.Value);
			if (requiredSites.Contains(site))
			{
				var title = $"Stop requiring recognized dependencies at {site} globally";
				context.RegisterCodeFix(
					CodeAction.Create(
						title,
						cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
							context.Document,
							snapshot.Source,
							document => TryRemoveRecognizedDependencySiteFromRoot(document, site),
							cancellationToken),
						title),
					diagnostic);
			}
		}

		var ancestorPaths = ConfigurationCodeFixSupport.GetAncestorLayerPaths(callerLayerName);
		var seenLayers = new HashSet<string>(StringComparer.Ordinal);
		foreach (var layerPath in ancestorPaths)
		{
			foreach (var snapshot in snapshots)
			{
				var layerElement = ConfigurationCodeFixSupport.FindLayerElement(snapshot.Document, layerPath);
				if (layerElement is null)
				{
					continue;
				}

				var requiredSites = ConfigurationCodeFixSupport.ReadSites(layerElement.Attribute("requireRecognizedDependencies")?.Value);
				if (!requiredSites.Contains(site) || !seenLayers.Add(layerPath))
				{
					continue;
				}

				var title = $"Stop requiring recognized dependencies at {site} for layer '{layerPath}'";
				context.RegisterCodeFix(
					CodeAction.Create(
						title,
						cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
							context.Document,
							snapshot.Source,
							document => TryRemoveRecognizedDependencySiteFromLayer(document, layerPath, site),
							cancellationToken),
						title),
					diagnostic);
			}
		}
	}

	private static bool TryAddTypeMatcher(XDocument document, string layerPath, string dependencyTypeName)
	{
		var layerElement = ConfigurationCodeFixSupport.FindLayerElement(document, layerPath);
		if (layerElement is null)
		{
			return false;
		}

		var existingMatcher = layerElement.Elements("Class")
			.Any(element =>
				string.Equals(element.Attribute("typeName")?.Value, dependencyTypeName, StringComparison.Ordinal)
				|| string.Equals(element.Attribute("exactName")?.Value, dependencyTypeName, StringComparison.Ordinal));
		if (existingMatcher)
		{
			return false;
		}

		layerElement.Add(new XElement("Class", new XAttribute("typeName", dependencyTypeName)));
		return true;
	}

	private static bool TryRemoveRecognizedDependencySiteFromRoot(XDocument document, string site)
	{
		var root = document.Root;
		if (root is null)
		{
			return false;
		}

		var result = TryRemoveRecognizedDependencySite(root, site);

		return result;
	}

	private static bool TryRemoveRecognizedDependencySiteFromLayer(XDocument document, string layerPath, string site)
	{
		var layerElement = ConfigurationCodeFixSupport.FindLayerElement(document, layerPath);
		if (layerElement is null)
		{
			return false;
		}

		var result = TryRemoveRecognizedDependencySite(layerElement, site);

		return result;
	}

	private static bool TryRemoveRecognizedDependencySite(XElement element, string site)
	{
		var sites = ConfigurationCodeFixSupport.ReadSites(element.Attribute("requireRecognizedDependencies")?.Value);
		if (!sites.Remove(site))
		{
			return false;
		}

		element.SetAttributeValue(
			"requireRecognizedDependencies",
			sites.Count == 0 ? null : ConfigurationCodeFixSupport.FormatSites(sites));
		return true;
	}
}
