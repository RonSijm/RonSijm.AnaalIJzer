using System.Text;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ArchitectureDocumentationInputAppender
{
	public static string Append(string documentation, string inputXml, string inputPath)
	{
		var sb = new StringBuilder(documentation.TrimEnd());
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("## Input Configuration");
		sb.AppendLine();
		sb.AppendLine($"This documentation was generated from the following architecture configuration: `{Escape(Path.GetFileName(inputPath))}`.");
		sb.AppendLine();
		sb.AppendLine("````xml");
		sb.Append(inputXml);
		if (!inputXml.EndsWith("\n", StringComparison.Ordinal) && !inputXml.EndsWith("\r", StringComparison.Ordinal))
		{
			sb.AppendLine();
		}
		sb.AppendLine("````");
		sb.AppendLine();
		return sb.ToString();
	}

	private static string Escape(string text)
	{
		var result = text.Replace("`", "\\`").Replace("\r", " ").Replace("\n", " ");

		return result;
	}
}
