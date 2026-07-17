using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Parsing;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ConfigurationDocumentationHost
{
	public static ConfigurationDocumentationResult Load(string configPath, CancellationToken cancellationToken)
	{
		var rootFile = LoadFile(configPath);
		var files = LoadFiles(rootFile, cancellationToken);
		var config = ArchitecturalConfigParser.ParseFile(rootFile, files, cancellationToken);
		var configDirectory = Path.GetDirectoryName(configPath)!;
		return new ConfigurationDocumentationResult(configDirectory, Path.GetFileName(configDirectory), config);
	}

	private static ImmutableArray<AdditionalText> LoadFiles(DiskAdditionalText rootFile, CancellationToken cancellationToken)
	{
		var files = ImmutableArray.CreateBuilder<AdditionalText>();
		var pending = new Queue<DiskAdditionalText>();
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		pending.Enqueue(rootFile);

		while (pending.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var file = pending.Dequeue();
			if (!visited.Add(file.Path))
			{
				continue;
			}

			files.Add(file);
			XDocument document;
			try
			{
				document = XDocument.Parse(file.Content, LoadOptions.SetLineInfo);
			}
			catch (System.Xml.XmlException)
			{
				continue;
			}
			foreach (var include in document.Root?.Elements().Where(element => element.Name.LocalName == "Include") ?? [])
			{
				var includePath = include.Attribute("path")?.Value;
				if (string.IsNullOrWhiteSpace(includePath))
				{
					continue;
				}

				var containingDirectory = Path.GetDirectoryName(file.Path)!;
				var resolvedPath = Path.GetFullPath(includePath, containingDirectory);
				if (File.Exists(resolvedPath))
				{
					pending.Enqueue(LoadFile(resolvedPath));
				}
			}
		}

		return files.ToImmutable();
	}

	private static DiskAdditionalText LoadFile(string path)
	{
		var fullPath = Path.GetFullPath(path);
		if (!File.Exists(fullPath))
		{
			throw new ToolingException($"Configuration file not found: {fullPath}");
		}

		return new DiskAdditionalText(fullPath, File.ReadAllText(fullPath));
	}

	private sealed class DiskAdditionalText(string path, string content) : AdditionalText
    {
		private readonly SourceText _text = SourceText.From(content);

        public string Content { get; } = content;
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = _text;

			return result;
		}
	}
}

internal sealed record ConfigurationDocumentationResult(string ConfigDirectory, string Title, AnalyzerConfiguration Config);
