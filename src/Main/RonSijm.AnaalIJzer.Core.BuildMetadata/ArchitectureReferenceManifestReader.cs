using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Config.Parsing;

namespace RonSijm.AnaalIJzer.BuildMetadata;

internal static class ArchitectureReferenceManifestReader
{
    internal static ArchitectureReferenceManifest Read(string content, string manifestPath, ImmutableArray<ConfigurationIssue>.Builder issues)
    {
        var normalizedContent = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalizedContent
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
        if (lines.Length == 0)
        {
            issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, "Architecture reference manifest is empty.", manifestPath, 0, 0));
            return ArchitectureReferenceManifest.Empty;
        }

        if (!string.Equals(lines[0].Trim(), ArchitectureReferenceManifest.Header, StringComparison.Ordinal))
        {
            issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Architecture reference manifest '{manifestPath}' has an unsupported header. Expected '{ArchitectureReferenceManifest.Header}'.", manifestPath, 1, 1));
            return ArchitectureReferenceManifest.Empty;
        }

        var seenErrors = new HashSet<string>(StringComparer.Ordinal);
        var projectReferences = ImmutableArray.CreateBuilder<ProjectReferenceManifestRecord>();
        var packageReferences = ImmutableArray.CreateBuilder<ArchitecturePackageReference>();
        for (var index = 1; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            if (string.Equals(parts[0], "Project", StringComparison.Ordinal)
                && parts.Length == 3
                && !string.IsNullOrWhiteSpace(parts[1])
                && string.IsNullOrWhiteSpace(parts[2]))
            {
                continue;
            }

            if (!TryParseRecord(parts, out var recordKind, out var projectRecord, out var packageRecord, out var error))
            {
                if (seenErrors.Add(error))
                {
                    issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, error, manifestPath, index + 1, 1));
                }

                continue;
            }

            if (recordKind == "Project")
            {
                projectReferences.Add(projectRecord);
            }

            if (recordKind == "Package")
            {
                packageReferences.Add(packageRecord);
            }
        }

        var result = new ArchitectureReferenceManifest(projectReferences.ToImmutable(), packageReferences.ToImmutable());

        return result;
    }

    private static bool TryParseRecord(
        string[] parts,
        out string recordKind,
        out ProjectReferenceManifestRecord projectRecord,
        out ArchitecturePackageReference packageRecord,
        out string error)
    {
        if (string.Equals(parts[0], "Project", StringComparison.Ordinal))
        {
            if (parts.Length != 3)
            {
                recordKind = string.Empty;
                projectRecord = default;
                packageRecord = default;
                error = "Project reference manifest entries must contain exactly three tab-delimited columns.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
            {
                recordKind = string.Empty;
                projectRecord = default;
                packageRecord = default;
                error = "Project reference manifest entries require both source and target project paths.";
                return false;
            }

            recordKind = "Project";
            projectRecord = new ProjectReferenceManifestRecord(parts[1].Trim(), parts[2].Trim());
            packageRecord = default;
            error = string.Empty;
            return true;
        }

        if (string.Equals(parts[0], "Package", StringComparison.Ordinal))
        {
            if (parts.Length != 5)
            {
                recordKind = string.Empty;
                projectRecord = default;
                packageRecord = default;
                error = "Package reference manifest entries must contain exactly five tab-delimited columns.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]) || string.IsNullOrWhiteSpace(parts[3]) || string.IsNullOrWhiteSpace(parts[4]))
            {
                recordKind = string.Empty;
                projectRecord = default;
                packageRecord = default;
                error = "Package reference manifest entries require source path, package ID, version, and directness.";
                return false;
            }

            if (!Enum.TryParse<PackageReferenceKind>(parts[4].Trim(), ignoreCase: false, out var referenceKind))
            {
                recordKind = string.Empty;
                projectRecord = default;
                packageRecord = default;
                error = $"Package reference manifest contains unsupported package reference kind '{parts[4].Trim()}'.";
                return false;
            }

            recordKind = "Package";
            projectRecord = default;
            packageRecord = new ArchitecturePackageReference(parts[1].Trim(), parts[2].Trim(), parts[3].Trim(), referenceKind);
            error = string.Empty;
            return true;
        }

        recordKind = string.Empty;
        projectRecord = default;
        packageRecord = default;
        error = $"Architecture reference manifest contains unsupported record kind '{parts[0]}'.";
        return false;
    }
}
