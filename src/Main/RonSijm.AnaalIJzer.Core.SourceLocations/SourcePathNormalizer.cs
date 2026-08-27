namespace RonSijm.AnaalIJzer.Core.SourceLocations;

public static class SourcePathNormalizer
{
	public static string NormalizeAbsolute(string filePath)
	{
		if (TryNormalizeAbsolutePath(filePath, out var normalizedAbsolutePath))
		{
			return normalizedAbsolutePath;
		}

		var fullPath = Path.GetFullPath(filePath);
		var result = NormalizeSeparators(fullPath);

		return result;
	}

	public static string NormalizeRelativeToBase(string filePath, string basePath)
	{
		if (TryNormalizeAbsolutePath(filePath, out var normalizedFilePath)
		    && TryNormalizeAbsolutePath(basePath, out var normalizedBasePath))
		{
			return GetRelativePath(normalizedBasePath, normalizedFilePath);
		}

		var fullPath = Path.GetFullPath(filePath);
		var fullBasePath = Path.GetFullPath(basePath);
		var normalizedFullPath = NormalizeSeparators(fullPath);
		var normalizedFullBasePath = NormalizeSeparators(fullBasePath);
		var relativePath = GetRelativePath(normalizedFullBasePath, normalizedFullPath);
		var result = NormalizeSeparators(relativePath);

		return result;
	}

	private static bool TryNormalizeAbsolutePath(string path, out string normalizedAbsolutePath)
	{
		var normalizedPath = NormalizeSeparators(path);
		if (IsWindowsDrivePath(normalizedPath))
		{
			var driveLetter = char.ToUpperInvariant(normalizedPath[0]);
			var remainder = normalizedPath.Substring(3).TrimStart('/');
			normalizedAbsolutePath = remainder.Length == 0 ? driveLetter + ":/" : driveLetter + ":/" + remainder;

			return true;
		}

		if (normalizedPath.StartsWith("//", StringComparison.Ordinal))
		{
			var trimmed = normalizedPath.TrimEnd('/');
			normalizedAbsolutePath = trimmed.Length == 0 ? "//" : trimmed;

			return true;
		}

		if (normalizedPath.StartsWith("/", StringComparison.Ordinal))
		{
			var trimmed = normalizedPath.TrimEnd('/');
			normalizedAbsolutePath = trimmed.Length == 0 ? "/" : trimmed;

			return true;
		}

		normalizedAbsolutePath = string.Empty;

		return false;
	}

	private static string GetRelativePath(string basePath, string fullPath)
	{
		var baseRoot = GetRoot(basePath);
		var fullRoot = GetRoot(fullPath);
		if (!string.Equals(baseRoot, fullRoot, GetRootComparison(baseRoot)))
		{
			return fullPath;
		}

		var baseSegments = SplitSegments(basePath, baseRoot);
		var fullSegments = SplitSegments(fullPath, fullRoot);
		var comparison = GetRootComparison(baseRoot);
		var commonLength = 0;
		while (commonLength < baseSegments.Length
		       && commonLength < fullSegments.Length
		       && string.Equals(baseSegments[commonLength], fullSegments[commonLength], comparison))
		{
			commonLength++;
		}

		var resultSegments = new List<string>();
		for (var i = commonLength; i < baseSegments.Length; i++)
		{
			resultSegments.Add("..");
		}

		for (var i = commonLength; i < fullSegments.Length; i++)
		{
			resultSegments.Add(fullSegments[i]);
		}

		var result = resultSegments.Count == 0 ? "." : string.Join("/", resultSegments);

		return result;
	}

	private static string GetRoot(string path)
	{
		if (IsWindowsDrivePath(path))
		{
			var result = char.ToUpperInvariant(path[0]) + ":/";

			return result;
		}

		if (path.StartsWith("//", StringComparison.Ordinal))
		{
			var segments = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length >= 2)
			{
				var result = "//" + segments[0] + "/" + segments[1] + "/";

				return result;
			}

			return "//";
		}

		if (path.StartsWith("/", StringComparison.Ordinal))
		{
			return "/";
		}

		return string.Empty;
	}

	private static StringComparison GetRootComparison(string root)
	{
		var result = root == "/" ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

		return result;
	}

	private static string[] SplitSegments(string path, string root)
	{
		var relativePath = root.Length == 0 ? path : path.Substring(root.Length);
		var result = relativePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

		return result;
	}

	private static bool IsWindowsDrivePath(string path)
	{
		var result = path.Length >= 3
		             && char.IsLetter(path[0])
		             && path[1] == ':'
		             && path[2] == '/';

		return result;
	}

	private static string NormalizeSeparators(string path)
	{
		var result = path.Replace('\\', '/');

		return result;
	}
}
