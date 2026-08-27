namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

public sealed class ArchitectureConfigurationTextDocument(string content, string path, bool isInlineConfiguration = false)
{
	public string Content { get; } = content;

	public string Path { get; } = path;

	public bool IsInlineConfiguration { get; } = isInlineConfiguration;
}
