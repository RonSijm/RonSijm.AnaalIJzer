using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Config;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Config.AnalyzerConfig;

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

	private sealed class DiskAdditionalText : AdditionalText
	{
		private readonly SourceText _text;

		public DiskAdditionalText(string path, string content)
		{
			Path = path;
			Content = content;
			_text = SourceText.From(content);
		}

		public string Content { get; }
		public override string Path { get; }
		public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
	}
}

internal sealed record ConfigurationDocumentationResult(string ConfigDirectory, string Title, AnalyzerConfiguration Config);
