using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;

internal sealed partial class ArchitectureSnapshotProvider
{
	private static string CreateConfigFingerprint(Project project, ImmutableArray<AdditionalText> additionalFiles)
	{
		var builder = new StringBuilder();
		builder.Append(project.FilePath ?? project.Name);
		builder.Append('|');
		builder.Append(project.Version.GetHashCode());
		foreach (var additionalFile in additionalFiles.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase))
		{
			builder.Append('|');
			builder.Append(additionalFile.Path);
			builder.Append(':');
			try
			{
				if (File.Exists(additionalFile.Path))
				{
					var info = new FileInfo(additionalFile.Path);
					builder.Append(info.Length);
					builder.Append('@');
					builder.Append(info.LastWriteTimeUtc.Ticks);
				}
				else
				{
					var text = additionalFile.GetText();
					builder.Append(text?.Length ?? 0);
					builder.Append('@');
					builder.Append(text?.ChecksumAlgorithm.ToString() ?? "none");
				}
			}
			catch (IOException)
			{
				builder.Append("unavailable");
			}
			catch (UnauthorizedAccessException)
			{
				builder.Append("unavailable");
			}
		}

		var result = builder.ToString();

		return result;
	}
}
