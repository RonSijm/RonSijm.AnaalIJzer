using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static HashSet<string> CollectDeclaredLayerPaths(IEnumerable<ArchitectureConfigurationElementInput> layerElements)
	{
		var paths = new HashSet<string>(StringComparer.Ordinal);
		CollectDeclaredLayerPaths(layerElements.Select(item => item.Element), string.Empty, paths);

		return paths;
	}

	private static void CollectDeclaredLayerPaths(IEnumerable<XElement> layerElements, string parentPath, ISet<string> paths)
	{
		foreach (var layerElement in layerElements)
		{
			var localName = layerElement.Attribute("name")?.Value;
			if (string.IsNullOrWhiteSpace(localName) || localName!.Contains('/'))
			{
				continue;
			}

			var validLocalName = localName!;
			var canonicalPath = string.IsNullOrEmpty(parentPath) ? validLocalName : parentPath + "/" + validLocalName;
			paths.Add(canonicalPath);
			CollectDeclaredLayerPaths(layerElement.Elements("Layer"), canonicalPath, paths);
		}
	}
}

