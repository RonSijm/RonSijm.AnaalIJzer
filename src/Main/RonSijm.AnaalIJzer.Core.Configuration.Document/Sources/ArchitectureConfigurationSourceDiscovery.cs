using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

public static class ArchitectureConfigurationSourceDiscovery
{
	public static ArchitectureConfigurationTextDocument? TryReadInlineConfigurationTextDocument(Compilation? compilation, string? fallbackPath, CancellationToken cancellationToken)
	{
		var xml = ArchitectureConfigurationDocumentLoader.TryReadInlineConfigurationXml(compilation);
		if (string.IsNullOrWhiteSpace(xml))
		{
			return null;
		}

		var sourcePath = compilation is null
			? fallbackPath
			: ArchitectureConfigurationDocumentLoader.FindInlineConfigurationSourcePath(compilation, fallbackPath, cancellationToken);

		string resolvedSourcePath;
		if (string.IsNullOrWhiteSpace(sourcePath))
		{
			resolvedSourcePath = fallbackPath ?? ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey;
		}
		else
		{
			resolvedSourcePath = sourcePath!;
		}

		var result = new ArchitectureConfigurationTextDocument(xml!, resolvedSourcePath, true);

		return result;
	}

	public static string? FindNearestConfigurationFilePath(string? path)
	{
		var directory = GetDirectory(path);
		while (!string.IsNullOrWhiteSpace(directory))
		{
			var candidate = Path.Combine(directory!, ArchitectureConfigurationDocumentLoader.ConfigFileName);
			if (File.Exists(candidate))
			{
				return candidate;
			}

			var parent = Directory.GetParent(directory!);
			if (parent is null)
			{
				break;
			}

			directory = parent.FullName;
		}

		return null;
	}

	public static ArchitectureConfigurationSource FindConfigurationSource(string? documentPath, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, CancellationToken cancellationToken)
	{
		var configFile = ArchitectureConfigurationDocumentLoader.FindConfigurationFile(additionalFiles);
		if (configFile is not null)
		{
			var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, configFile.Path);

			return source;
		}

		var inlineDocument = TryReadInlineConfigurationTextDocument(compilation, documentPath, cancellationToken);
		if (inlineDocument is not null)
		{
			var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, inlineDocument.Path);

			return source;
		}

		return ArchitectureConfigurationSource.None;
	}

	public static bool TryCreateConfigurationSource(string? configurationFilePath, string? inlineConfigurationSourcePath, out ArchitectureConfigurationSource source)
	{
		if (!string.IsNullOrWhiteSpace(configurationFilePath))
		{
			var resolvedConfigurationFilePath = configurationFilePath!;
			if (File.Exists(resolvedConfigurationFilePath))
			{
				source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, resolvedConfigurationFilePath);

				return true;
			}
		}

		if (!string.IsNullOrWhiteSpace(inlineConfigurationSourcePath))
		{
			var resolvedInlineConfigurationSourcePath = inlineConfigurationSourcePath!;
			if (File.Exists(resolvedInlineConfigurationSourcePath))
			{
				source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, resolvedInlineConfigurationSourcePath);

				return true;
			}
		}

		source = ArchitectureConfigurationSource.None;

		return false;
	}

	public static ImmutableArray<ArchitectureConfigurationCreationTarget> CreateConfigurationCreationTargets(string? projectFilePath, string? solutionFilePath)
	{
		var builder = ImmutableArray.CreateBuilder<ArchitectureConfigurationCreationTarget>();
		var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var projectDirectory = GetDirectory(projectFilePath);
		var solutionDirectory = GetDirectory(solutionFilePath);
		AddCreationTarget(
			builder,
			seenTargets,
			"Project file",
			"Create Architecture.anl next to the current project file and add it as an <AdditionalFiles> item in this .csproj.",
			projectDirectory,
			ArchitectureConfigurationRegistrationKind.ProjectFile,
			projectFilePath);
		AddCreationTarget(
			builder,
			seenTargets,
			"Project folder",
			"Create Architecture.anl next to the current project file and register it from Directory.Build.props for projects in this folder.",
			projectDirectory,
			ArchitectureConfigurationRegistrationKind.DirectoryBuildProps,
			CreateDirectoryBuildPropsPath(projectDirectory));
		AddCreationTarget(
			builder,
			seenTargets,
			"Solution folder",
			"Create Architecture.anl next to the solution file and register it from Directory.Build.props for projects under the solution folder.",
			solutionDirectory,
			ArchitectureConfigurationRegistrationKind.DirectoryBuildProps,
			CreateDirectoryBuildPropsPath(solutionDirectory));
		var result = builder.ToImmutable();

		return result;
	}

	private static void AddCreationTarget(
		ImmutableArray<ArchitectureConfigurationCreationTarget>.Builder builder,
		HashSet<string> seenTargets,
		string title,
		string description,
		string? directory,
		ArchitectureConfigurationRegistrationKind registrationKind,
		string? registrationPath)
	{
		if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(registrationPath))
		{
			return;
		}

		var path = CombinePathPreservingStyle(directory!, ArchitectureConfigurationDocumentLoader.ConfigFileName);
		var key = path + "|" + registrationKind + "|" + registrationPath;
		if (!seenTargets.Add(key))
		{
			return;
		}

		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);
		builder.Add(new ArchitectureConfigurationCreationTarget(title, description, source, registrationKind, registrationPath!));
	}

	private static string? CreateDirectoryBuildPropsPath(string? directory)
	{
		var result = string.IsNullOrWhiteSpace(directory) ? null : CombinePathPreservingStyle(directory!, "Directory.Build.props");

		return result;
	}

	private static string? GetDirectory(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		if (Directory.Exists(path))
		{
			return path;
		}

		var inputPath = path!;
		var normalizedExistingPath = NormalizeForCurrentPlatform(inputPath);
		if (!string.Equals(normalizedExistingPath, inputPath, StringComparison.Ordinal)
		    && Directory.Exists(normalizedExistingPath))
		{
			return normalizedExistingPath;
		}

		var trimmedPath = TrimEndingSeparators(inputPath);
		var separatorIndex = trimmedPath.LastIndexOfAny(['\\', '/']);
		if (separatorIndex < 0)
		{
			return null;
		}

		if (separatorIndex == 0)
		{
			return trimmedPath.Substring(0, 1);
		}

		if (separatorIndex == 2 && IsWindowsDrivePath(trimmedPath))
		{
			return trimmedPath.Substring(0, 3);
		}

		var result = trimmedPath.Substring(0, separatorIndex);

		return result;
	}

	private static string CombinePathPreservingStyle(string directory, string fileName)
	{
		var trimmedDirectory = TrimEndingSeparators(directory);
		var separator = GetPreferredSeparator(directory);
		var result = trimmedDirectory + separator + fileName;

		return result;
	}

	private static string NormalizeForCurrentPlatform(string path)
	{
		var result = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

		return result;
	}

	private static string TrimEndingSeparators(string path)
	{
		if (path.Length <= 1)
		{
			return path;
		}

		if (path.Length == 3 && IsWindowsDrivePath(path))
		{
			return path;
		}

		var result = path.TrimEnd('\\', '/');

		return string.IsNullOrEmpty(result) ? path : result;
	}

	private static char GetPreferredSeparator(string path)
	{
		if (path.Contains('\\') && !path.Contains('/'))
		{
			return '\\';
		}

		if (path.Contains('/') && !path.Contains('\\'))
		{
			return '/';
		}

		return Path.DirectorySeparatorChar;
	}

	private static bool IsWindowsDrivePath(string path)
	{
		var result = path.Length >= 3
		             && char.IsLetter(path[0])
		             && path[1] == ':'
		             && (path[2] == '\\' || path[2] == '/');

		return result;
	}
}
