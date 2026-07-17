namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationLayerPaths
{
	internal static string[] SplitLayerPath(string layerPath)
	{
		var result = layerPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

		return result;
	}

	internal static int GetCommonPrefixLength(string[] left, string[] right)
	{
		var count = Math.Min(left.Length, right.Length);
		for (var index = 0; index < count; index++)
		{
			if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
			{
				return index;
			}
		}

		return count;
	}

	internal static string FormatRootLayerReference(string layerPath)
	{
		if (layerPath == "*" || !layerPath.Contains("/"))
		{
			return layerPath;
		}

		var result = "/" + layerPath;

		return result;
	}

	internal static string FormatScopeName(string scopePath)
	{
		var result = string.IsNullOrWhiteSpace(scopePath) ? "root" : scopePath;

		return result;
	}
}
