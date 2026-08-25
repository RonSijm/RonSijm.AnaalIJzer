using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;

internal sealed partial class ArchitectureSnapshotProvider
{
	private readonly struct SnapshotCacheKey(
		Guid documentId,
		int versionNumber,
		int projectVersion,
		string configFingerprint,
		bool includeCodeEvidence)
		: IEquatable<SnapshotCacheKey>
	{
		public Guid DocumentId { get; } = documentId;

		public int VersionNumber { get; } = versionNumber;

		public int ProjectVersion { get; } = projectVersion;

		public string ConfigFingerprint { get; } = configFingerprint;

		public bool IncludeCodeEvidence { get; } = includeCodeEvidence;

		public bool Equals(SnapshotCacheKey other)
		{
			var result = DocumentId.Equals(other.DocumentId)
			             && VersionNumber == other.VersionNumber
			             && ProjectVersion == other.ProjectVersion
			             && string.Equals(ConfigFingerprint, other.ConfigFingerprint, StringComparison.Ordinal)
			             && IncludeCodeEvidence == other.IncludeCodeEvidence;

			return result;
		}

		public override bool Equals(object? obj)
		{
			var result = obj is SnapshotCacheKey other && Equals(other);

			return result;
		}

		public override int GetHashCode()
		{
			var result = unchecked((((DocumentId.GetHashCode() * 397) ^ VersionNumber) * 397) ^ ProjectVersion);
			result = unchecked((result * 397) ^ StringComparer.Ordinal.GetHashCode(ConfigFingerprint));
			result = unchecked((result * 397) ^ IncludeCodeEvidence.GetHashCode());

			return result;
		}
	}

	private sealed class PhysicalAdditionalText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText? GetText(CancellationToken cancellationToken = default)
		{
			var result = SourceText.From(File.ReadAllText(Path), Encoding.UTF8);

			return result;
		}
	}
}
