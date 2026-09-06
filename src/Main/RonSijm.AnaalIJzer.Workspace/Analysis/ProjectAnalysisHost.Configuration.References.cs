using System.Collections.Immutable;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;
using RonSijm.AnaalIJzer.Workspace.Support;

namespace RonSijm.AnaalIJzer.Workspace.Analysis;

internal sealed partial class ProjectAnalysisHost
{
	private static ImmutableArray<AdditionalText> GetEffectiveAdditionalFiles(
		ImmutableArray<AdditionalText> additionalFiles,
		ImmutableArray<AdditionalText> supplementalConfigFiles,
		ArchitectureReferenceManifest? referenceManifest,
		string projectFilePath)
	{
		var result = additionalFiles;
		if (supplementalConfigFiles.Length > 0)
		{
			foreach (var supplementalConfigFile in supplementalConfigFiles)
			{
				if (ContainsAdditionalFile(result, supplementalConfigFile.Path))
				{
					continue;
				}

				result = result.Add(supplementalConfigFile);
			}
		}

		if (referenceManifest is { } manifest)
		{
			result = result.RemoveAll(file => string.Equals(Path.GetFileName(file.Path), ArchitectureReferenceManifest.FileName, StringComparison.OrdinalIgnoreCase));
			result = result.Add(CreateProjectReferenceManifestAdditionalText(manifest, projectFilePath));
		}

		return result;
	}

	internal static ImmutableArray<AdditionalText> GetSupplementalConfigurationFiles(string projectFilePath, ImmutableArray<AdditionalText> additionalFiles, string? inlineConfigXml, CancellationToken cancellationToken, string? seedConfigPath = null)
	{
		if (inlineConfigXml is not null)
		{
			return [];
		}

		var configuredConfigPath = ArchitecturalConfigParser.FindConfigFile(additionalFiles)?.Path;
		var effectiveSeedConfigPath = string.IsNullOrWhiteSpace(seedConfigPath)
			? configuredConfigPath ?? ArchitectureConfigurationSourceDiscovery.FindNearestConfigurationFilePath(projectFilePath)
			: seedConfigPath;
		if (string.IsNullOrWhiteSpace(effectiveSeedConfigPath) || !File.Exists(effectiveSeedConfigPath))
		{
			return [];
		}

		var results = ImmutableArray.CreateBuilder<AdditionalText>();
		var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		AddFallbackConfigurationFile(results, seenPaths, effectiveSeedConfigPath, cancellationToken);

		var configDirectory = Path.GetDirectoryName(effectiveSeedConfigPath);
		if (string.IsNullOrWhiteSpace(configDirectory))
		{
			return results.ToImmutable();
		}

		foreach (var directory in EnumerateFallbackConfigurationDirectories(configDirectory))
		{
			foreach (var path in Directory.EnumerateFiles(directory, "*.anl", SearchOption.TopDirectoryOnly))
			{
				AddFallbackConfigurationFile(results, seenPaths, path, cancellationToken);
			}
		}

		return results.ToImmutable();
	}

	internal static ImmutableArray<AdditionalText> NormalizeProjectAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles, string projectDirectory, CancellationToken cancellationToken)
	{
		if (additionalFiles.Length == 0)
		{
			return additionalFiles;
		}

		var results = ImmutableArray.CreateBuilder<AdditionalText>(additionalFiles.Length);
		foreach (var additionalFile in additionalFiles)
		{
			var normalizedPath = NormalizeProjectAdditionalFilePath(additionalFile.Path, projectDirectory);
			if (string.Equals(normalizedPath, additionalFile.Path, StringComparison.Ordinal))
			{
				results.Add(additionalFile);

				continue;
			}

			var content = additionalFile.GetText(cancellationToken)?.ToString() ?? string.Empty;
			results.Add(WorkspaceAdditionalText.FromText(normalizedPath, content));
		}

		var result = results.ToImmutable();

		return result;
	}

	private static IEnumerable<string> EnumerateFallbackConfigurationDirectories(string configDirectory)
	{
		yield return configDirectory;

		var parent = Directory.GetParent(configDirectory);
		if (parent is not null)
		{
			yield return parent.FullName;
		}
	}

	private static void AddFallbackConfigurationFile(ImmutableArray<AdditionalText>.Builder results, HashSet<string> seenPaths, string path, CancellationToken cancellationToken)
	{
		var normalizedPath = ArchitectureConfigurationSourceLookup.NormalizePath(path);
		if (!seenPaths.Add(normalizedPath))
		{
			return;
		}

		results.Add(WorkspaceAdditionalText.FromFile(path, cancellationToken));
	}

	private static bool ContainsAdditionalFile(ImmutableArray<AdditionalText> additionalFiles, string path)
	{
		var normalizedPath = ArchitectureConfigurationSourceLookup.NormalizePath(path);
		var result = additionalFiles.Any(file => string.Equals(ArchitectureConfigurationSourceLookup.NormalizePath(file.Path), normalizedPath, StringComparison.OrdinalIgnoreCase));

		return result;
	}

	private static string NormalizeProjectAdditionalFilePath(string path, string projectDirectory)
	{
		if (ArchitectureConfigurationSourceLookup.IsPathRootedPreservingStyle(path))
		{
			return ArchitectureConfigurationSourceLookup.NormalizePath(path);
		}

		var platformRelativePath = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
		var resolvedExistingPath = TryResolveExistingAdditionalFilePath(platformRelativePath, projectDirectory);
		if (!string.IsNullOrWhiteSpace(resolvedExistingPath))
		{
			return ArchitectureConfigurationSourceLookup.NormalizePath(resolvedExistingPath);
		}

		var combinedPath = Path.Combine(projectDirectory, platformRelativePath);
		var result = ArchitectureConfigurationSourceLookup.NormalizePath(combinedPath);

		return result;
	}

	private static string? TryResolveExistingAdditionalFilePath(string path, string projectDirectory)
	{
		var candidatePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddExistingPathCandidate(candidatePaths, Path.GetFullPath(Path.Combine(projectDirectory, path)));

		if (Path.GetExtension(path).Equals(".anl", StringComparison.OrdinalIgnoreCase))
		{
			var fileName = Path.GetFileName(path);
			foreach (var directory in EnumerateFallbackConfigurationDirectories(projectDirectory))
			{
				foreach (var candidate in Directory.EnumerateFiles(directory, fileName, SearchOption.TopDirectoryOnly))
				{
					AddExistingPathCandidate(candidatePaths, candidate);
				}
			}
		}

		var bestCandidate = candidatePaths
			.OrderByDescending(candidate => GetPathSuffixMatchScore(path, candidate))
			.ThenBy(candidate => candidate.Length)
			.FirstOrDefault();

		return bestCandidate;
	}

	private static void AddExistingPathCandidate(HashSet<string> candidatePaths, string path)
	{
		if (!File.Exists(path))
		{
			return;
		}

		candidatePaths.Add(path);
	}

	private static int GetPathSuffixMatchScore(string rawPath, string candidatePath)
	{
		var rawSegments = rawPath.Replace('\\', '/').Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
		var candidateSegments = ArchitectureConfigurationSourceLookup.NormalizePath(candidatePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
		var score = 0;
		var maxComparisons = Math.Min(rawSegments.Length, candidateSegments.Length);
		for (var index = 1; index <= maxComparisons; index++)
		{
			var rawSegment = rawSegments[^index];
			var candidateSegment = candidateSegments[^index];
			if (!string.Equals(rawSegment, candidateSegment, StringComparison.OrdinalIgnoreCase))
			{
				break;
			}

			score++;
		}

		return score;
	}

	private static ArchitectureReferenceManifest? CreateProjectReferenceManifest(Project project)
	{
		if (string.IsNullOrWhiteSpace(project.FilePath))
		{
			return null;
		}

		var projectReferences = ReadDirectProjectReferences(project.FilePath)
			.Select(referencePath => new ProjectReferenceManifestRecord(project.FilePath, referencePath))
			.ToImmutableArray();
		var packageReferences = ReadResolvedPackageReferences(project.FilePath).ToImmutableArray();
		var assemblyReferences = ReadDirectAssemblyReferences(project.FilePath).ToImmutableArray();
		var result = new ArchitectureReferenceManifest(projectReferences, packageReferences, assemblyReferences);

		return result;
	}

	private static AdditionalText CreateProjectReferenceManifestAdditionalText(ArchitectureReferenceManifest manifest, string projectFilePath)
	{
		var manifestPath = Path.Combine(Path.GetDirectoryName(projectFilePath) ?? Directory.GetCurrentDirectory(), ArchitectureReferenceManifest.FileName);
		var manifestContent = ArchitectureReferenceManifestWriter.Write(manifest);
		var result = WorkspaceAdditionalText.FromText(manifestPath, manifestContent);

		return result;
	}

	private static IEnumerable<string> ReadDirectProjectReferences(string projectFilePath)
	{
		var document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);
		var projectDirectory = Path.GetDirectoryName(projectFilePath) ?? Directory.GetCurrentDirectory();
		foreach (var element in document.Descendants().Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal)))
		{
			var include = element.Attribute("Include")?.Value;
			if (string.IsNullOrWhiteSpace(include))
			{
				continue;
			}

			var referenceOutputAssembly = element.Attribute("ReferenceOutputAssembly")?.Value;
			if (string.Equals(referenceOutputAssembly, "false", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var platformRelativePath = include.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
			var resolvedProjectPath = Path.GetFullPath(Path.Combine(projectDirectory, platformRelativePath));

			yield return resolvedProjectPath;
		}
	}

	private static IEnumerable<ArchitecturePackageReference> ReadResolvedPackageReferences(string projectFilePath)
	{
		var projectDirectory = Path.GetDirectoryName(projectFilePath) ?? Directory.GetCurrentDirectory();
		var projectAssetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
		if (!File.Exists(projectAssetsPath))
		{
			yield break;
		}

		var directPackageIds = ReadDirectPackageIds(projectFilePath);
		var seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		using var document = JsonDocument.Parse(File.ReadAllText(projectAssetsPath));
		if (!document.RootElement.TryGetProperty("targets", out var targets))
		{
			yield break;
		}

		foreach (var target in targets.EnumerateObject())
		{
			foreach (var library in target.Value.EnumerateObject())
			{
				if (!library.Value.TryGetProperty("type", out var typeElement) || !string.Equals(typeElement.GetString(), "package", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				var hasCompile = library.Value.TryGetProperty("compile", out var compileElement) && compileElement.ValueKind == JsonValueKind.Object && compileElement.EnumerateObject().Any();
				var hasRuntime = library.Value.TryGetProperty("runtime", out var runtimeElement) && runtimeElement.ValueKind == JsonValueKind.Object && runtimeElement.EnumerateObject().Any();
				if (!hasCompile && !hasRuntime)
				{
					continue;
				}

				var slashIndex = library.Name.LastIndexOf('/');
				if (slashIndex <= 0 || slashIndex == library.Name.Length - 1)
				{
					continue;
				}

				var packageId = library.Name[..slashIndex];
				var packageVersion = library.Name[(slashIndex + 1)..];
				var referenceKind = directPackageIds.Contains(packageId) ? PackageReferenceKind.Direct : PackageReferenceKind.Transitive;
				var key = packageId + "|" + packageVersion + "|" + referenceKind;
				if (!seenPackages.Add(key))
				{
					continue;
				}

				yield return new ArchitecturePackageReference(projectFilePath, packageId, packageVersion, referenceKind);
			}
		}
	}

	private static IEnumerable<ArchitectureAssemblyReference> ReadDirectAssemblyReferences(string projectFilePath)
	{
		var document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);
		foreach (var element in document.Descendants().Where(element => string.Equals(element.Name.LocalName, "Reference", StringComparison.Ordinal)))
		{
			var include = element.Attribute("Include")?.Value;
			if (string.IsNullOrWhiteSpace(include))
			{
				continue;
			}

			var assemblyIdentity = GetAssemblyIdentity(include!);
			if (string.IsNullOrWhiteSpace(assemblyIdentity))
			{
				continue;
			}

			var hintPath = element.Element(element.Name.Namespace + "HintPath")?.Value;
			if (string.IsNullOrWhiteSpace(hintPath))
			{
				hintPath = element.Attribute("HintPath")?.Value;
			}

			yield return new ArchitectureAssemblyReference(projectFilePath, assemblyIdentity, string.IsNullOrWhiteSpace(hintPath) ? null : hintPath.Trim());
		}
	}

	private static string GetAssemblyIdentity(string include)
	{
		var commaIndex = include.IndexOf(',');
		var result = (commaIndex < 0 ? include : include[..commaIndex]).Trim();

		return result;
	}

	private static HashSet<string> ReadDirectPackageIds(string projectFilePath)
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);
		foreach (var element in document.Descendants().Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal)))
		{
			var include = element.Attribute("Include")?.Value;
			if (string.IsNullOrWhiteSpace(include))
			{
				continue;
			}

			result.Add(include);
		}

		return result;
	}
}
