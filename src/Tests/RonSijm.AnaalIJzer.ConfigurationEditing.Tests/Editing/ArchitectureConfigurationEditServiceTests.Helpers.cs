using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	private static ArchitectureDependencyRuleEditHandle CreateHandle(
		string path,
		string elementKind,
		string from,
		string to,
		ArchitectureConfigurationSourceKind sourceKind = ArchitectureConfigurationSourceKind.XmlFile)
	{
		var result = new ArchitectureDependencyRuleEditHandle(
			sourceKind,
			path,
			0,
			0,
			elementKind,
			string.Empty,
			from,
			to,
			from,
			to,
			false);

		return result;
	}

	private static ImmutableDictionary<string, string> Attributes(params (string Key, string Value)[] attributes)
	{
		var result = attributes.ToImmutableDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.Ordinal);

		return result;
	}

	private sealed class TemporaryDirectory : IDisposable
	{
		private readonly string path = Path.Combine(Path.GetTempPath(), "AnaalIJzerVsixTests", Guid.NewGuid().ToString("N"));

		public string WriteFile(string fileName, string content, Encoding? encoding = null)
		{
			Directory.CreateDirectory(path);
			var filePath = Path.Combine(path, fileName);
			File.WriteAllText(filePath, content, encoding ?? Encoding.UTF8);

			return filePath;
		}

		public void Dispose()
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}
