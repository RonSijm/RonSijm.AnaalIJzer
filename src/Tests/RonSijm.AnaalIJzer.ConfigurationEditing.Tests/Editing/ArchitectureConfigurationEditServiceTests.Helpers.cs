using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

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
		private readonly string _path = Path.Combine(Path.GetTempPath(), "AnaalIJzerVsixTests", Guid.NewGuid().ToString("N"));

		public string WriteFile(string fileName, string content, Encoding? encoding = null)
		{
			Directory.CreateDirectory(_path);
			var filePath = Path.Combine(_path, fileName);
			File.WriteAllText(filePath, content, encoding ?? Encoding.UTF8);

			return filePath;
		}

		public string GetPath(string fileName)
		{
			Directory.CreateDirectory(_path);
			var result = Path.Combine(_path, fileName);

			return result;
		}

		public void Dispose()
		{
			if (Directory.Exists(_path))
			{
				Directory.Delete(_path, true);
			}
		}
	}
}
