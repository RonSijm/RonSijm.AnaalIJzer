using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static partial class ArchitectureConfigurationFileService
{
	private static IReadOnlyList<GraphComponent> FindGraphComponents(IReadOnlyList<ConfigurationElement> elements)
	{
		var layerNames = elements
			.Where(item => item.Element.Name.LocalName == "Layer")
			.Select(item => item.Element.Attribute("name")?.Value)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Cast<string>()
			.Distinct(StringComparer.Ordinal)
			.ToList();

		if (layerNames.Count == 0)
		{
			throw new ArchitectureConfigurationFileOperationException("The configuration does not define any dependency graph nodes.");
		}

		var sets = new DisjointSet(layerNames);
		foreach (var item in elements)
		{
			if (item.Element.Name.LocalName is "AllowedDependency" or "BlockedDependency")
			{
				UnionEdgeRoots(item.Element, null, layerNames, sets);
			}
			else if (item.Element.Name.LocalName == "Layer" && item.Element.Attribute("name")?.Value is { } ownerRoot)
			{
				foreach (var edge in item.Element.Descendants().Where(element => element.Name.LocalName is "AllowedDependency" or "BlockedDependency"))
				{
					UnionEdgeRoots(edge, ownerRoot, layerNames, sets);
				}
			}
		}

		var componentNames = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		foreach (var layerName in layerNames)
		{
			var root = sets.Find(layerName);
			if (!componentNames.TryGetValue(root, out var names))
			{
				names = [];
				componentNames.Add(root, names);
			}

			names.Add(layerName);
		}

		var components = componentNames.Select(pair => new GraphComponent(pair.Value, [])).ToList();
		var componentsByLayer = components.SelectMany(component => component.LayerNames.Select(name => (name, component)))
			.ToDictionary(item => item.name, item => item.component, StringComparer.Ordinal);
		foreach (var item in elements)
		{
			GraphComponent? component = null;
			if (item.Element.Name.LocalName == "Layer" && item.Element.Attribute("name")?.Value is { } layerName)
			{
				componentsByLayer.TryGetValue(layerName, out component);
			}
			else if (item.Element.Name.LocalName is "AllowedDependency" or "BlockedDependency")
			{
				var from = item.Element.Attribute("from")?.Value;
				var to = item.Element.Attribute("to")?.Value;
				var namedEndpoint = GetRootReference(from) ?? GetRootReference(to);
				if (from == "*" && to == "*")
				{
					namedEndpoint = layerNames[0];
				}

				if (namedEndpoint is not null)
				{
					componentsByLayer.TryGetValue(namedEndpoint, out component);
				}
			}

			component?.Elements.Add(item);
		}

		return components;
	}

	private static void UnionEdgeRoots(XElement edge, string? ownerRoot, IReadOnlyCollection<string> layerNames, DisjointSet sets)
	{
		var fromReference = edge.Attribute("from")?.Value;
		var toReference = edge.Attribute("to")?.Value;
		var from = ownerRoot is not null && fromReference?.StartsWith("/", StringComparison.Ordinal) != true ? ownerRoot : GetRootReference(fromReference);
		var to = ownerRoot is not null && toReference?.StartsWith("/", StringComparison.Ordinal) != true ? ownerRoot : GetRootReference(toReference);
		if (edge.Attribute("from")?.Value == "*" || edge.Attribute("to")?.Value == "*")
		{
			foreach (var layerName in layerNames)
			{
				if (ownerRoot is not null)
				{
					sets.Union(ownerRoot, layerName);
				}
			}

			return;
		}

		if (from is not null && to is not null && layerNames.Contains(from) && layerNames.Contains(to))
		{
			sets.Union(from, to);
		}
	}

	private static string? GetRootReference(string? reference)
	{
		if (string.IsNullOrWhiteSpace(reference) || reference == "*")
		{
			return null;
		}

		var normalized = reference!.TrimStart('/');
		var separator = normalized.IndexOf('/');
		var result = separator < 0 ? normalized : normalized.Substring(0, separator);

		return result;
	}

	private static string CreateGraphFileName(int index, IReadOnlyList<string> layerNames)
	{
		var descriptiveName = string.Join("-", layerNames.Take(3).Select(SanitizeFileName));
		if (string.IsNullOrWhiteSpace(descriptiveName))
		{
			descriptiveName = "Unnamed";
		}

		if (descriptiveName.Length > 72)
		{
			descriptiveName = descriptiveName.Substring(0, 72).TrimEnd('-');
		}

		var result = $"Graph.{index + 1:D2}.{descriptiveName}.anl";

		return result;
	}

	private static string SanitizeFileName(string value)
	{
		var invalidCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());
		var characters = value.Select(character => invalidCharacters.Contains(character) || char.IsWhiteSpace(character) ? '-' : character).ToArray();
		var result = new string(characters).Trim('-');

		return result;
	}

	private sealed class GraphComponent
	{
		public GraphComponent(IReadOnlyList<string> layerNames, List<ConfigurationElement> elements)
		{
			LayerNames = layerNames;
			Elements = elements;
		}

		public IReadOnlyList<string> LayerNames { get; }

		public List<ConfigurationElement> Elements { get; }
	}

	private sealed class DisjointSet
	{
		private readonly Dictionary<string, string> parents;

		public DisjointSet(IEnumerable<string> values)
		{
			parents = values.ToDictionary(value => value, value => value, StringComparer.Ordinal);
		}

		public string Find(string value)
		{
			if (!parents.TryGetValue(value, out var parent))
			{
				parents[value] = value;

				return value;
			}

			if (parent != value)
			{
				parents[value] = Find(parent);
			}

			var result = parents[value];

			return result;
		}

		public void Union(string left, string right)
		{
			var leftRoot = Find(left);
			var rightRoot = Find(right);
			if (leftRoot != rightRoot)
			{
				parents[rightRoot] = leftRoot;
			}
		}
	}
}
