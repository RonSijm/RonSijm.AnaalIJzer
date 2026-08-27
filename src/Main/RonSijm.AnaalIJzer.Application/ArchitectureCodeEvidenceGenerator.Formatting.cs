using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureCodeEvidenceGenerator
{
	private static string FormatLocation(Location location, string projectDirectory)
	{
		if (!location.IsInSource)
		{
			return string.Empty;
		}

		var span = location.GetLineSpan();
		var path = span.Path;
		if (!string.IsNullOrWhiteSpace(path))
		{
			try
			{
				path = Path.GetRelativePath(projectDirectory, path);
			}
			catch (ArgumentException)
			{
			}
			catch (NotSupportedException)
			{
			}
			catch (PathTooLongException)
			{
			}
		}

		var result = $"{path}:{span.StartLinePosition.Line + 1}";

		return result;
	}

	private static string GetTypeName(INamedTypeSymbol type)
	{
		var result = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

		return result;
	}

	private static string NormalizePath(string path)
	{
		try
		{
			var result = Path.GetFullPath(path);

			return result;
		}
		catch (ArgumentException)
		{
			return path;
		}
		catch (NotSupportedException)
		{
			return path;
		}
		catch (PathTooLongException)
		{
			return path;
		}
	}

	private static string Escape(string text)
	{
		var result = text.Replace("\r", " ").Replace("\n", " ").Replace("`", "\\`");

		return result;
	}

	private static string EscapeTable(string text)
	{
		var result = text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Replace("`", "\\`");

		return result;
	}
}

