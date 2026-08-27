
namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureDependencyInsertionPlanner
{
	internal static DependencyInsertion CreateDependencyInsertion(string from, string to)
	{
		if (from == "*" || to == "*")
		{
			return new DependencyInsertion(string.Empty, from, to);
		}

		var fromParts = ArchitectureConfigurationLayerPaths.SplitLayerPath(from);
		var toParts = ArchitectureConfigurationLayerPaths.SplitLayerPath(to);
		var commonLength = ArchitectureConfigurationLayerPaths.GetCommonPrefixLength(fromParts, toParts);
		var areDirectSiblings = fromParts.Length == commonLength + 1 && toParts.Length == commonLength + 1;
		if (areDirectSiblings)
		{
			var scopePath = string.Join("/", fromParts.Take(commonLength));
			var result = new DependencyInsertion(scopePath, fromParts[fromParts.Length - 1], toParts[toParts.Length - 1]);

			return result;
		}

		var rootResult = new DependencyInsertion(string.Empty, ArchitectureConfigurationLayerPaths.FormatRootLayerReference(from), ArchitectureConfigurationLayerPaths.FormatRootLayerReference(to));

		return rootResult;
	}

	internal readonly struct DependencyInsertion(string scopePath, string configuredFrom, string configuredTo)
	{
		public string ScopePath { get; } = scopePath;

		public string ConfiguredFrom { get; } = configuredFrom;

		public string ConfiguredTo { get; } = configuredTo;
	}
}
