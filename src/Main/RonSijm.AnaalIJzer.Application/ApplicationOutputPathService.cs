using System.Text;

namespace RonSijm.AnaalIJzer.Application;

internal static class ApplicationOutputPathService
{
	public static async Task WriteOutputAsync(string outputPath, string content, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(outputPath) && !force)
		{
			throw new ApplicationOperationException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		await File.WriteAllTextAsync(outputPath, content, new UTF8Encoding(false), cancellationToken);
	}

	public static string ResolveOutputPath(string? requestedPath, string fallbackPath, string workingDirectory)
	{
		if (!string.IsNullOrWhiteSpace(requestedPath))
		{
			return Path.GetFullPath(requestedPath);
		}

		var result = Path.GetFullPath(Path.IsPathRooted(fallbackPath) ? fallbackPath : Path.Combine(workingDirectory, fallbackPath));

		return result;
	}

	public static string EnsureFinalNewLine(string text)
	{
		var result = text.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? text : text + Environment.NewLine;

		return result;
	}

	public static string NormalizeLineEndings(string text)
	{
		var result = text.Replace("\r\n", "\n").Replace('\r', '\n');

		return result;
	}
}

