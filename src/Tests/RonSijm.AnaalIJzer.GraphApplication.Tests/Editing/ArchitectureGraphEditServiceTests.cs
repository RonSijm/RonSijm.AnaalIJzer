using System.Text;

namespace RonSijm.AnaalIJzer.GraphApplication.Tests.Editing;

public sealed class ArchitectureGraphEditServiceTests
{
	[Fact]
	public void CreateConfiguration_WritesArchitectureFile()
	{
		using var directory = new TemporaryDirectory();
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, directory.GetPath("Architecture.anl"));
		var service = new ArchitectureGraphEditService();

		var result = service.CreateConfiguration(source);

		result.Succeeded.Should().BeTrue(result.Message);
		File.Exists(source.Path).Should().BeTrue();
		File.ReadAllText(source.Path).Should().Contain("<ArchitecturalLevels");
	}

	[Fact]
	public void AddAllowedDependency_AppendsRuleToCreatedConfiguration()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);
		var service = new ArchitectureGraphEditService();

		var result = service.AddAllowedDependency(source, "Customer", "Waiter");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<AllowedDependency from=\"Customer\" to=\"Waiter\" />");
	}

	[Fact]
	public void SetDependencySites_PersistsAllowedSitesThroughGraphEditService()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" />
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureDependencyRuleEditHandle(
			ArchitectureConfigurationSourceKind.XmlFile,
			path,
			0,
			0,
			"AllowedDependency",
			string.Empty,
			"Customer",
			"Waiter",
			"Customer",
			"Waiter",
			false);
		var service = new ArchitectureGraphEditService();

		var result = service.SetDependencySites(
			handle,
			ArchitectureSiteFilterEditMode.AllowedSites,
			ImmutableArray.Create(ArchitectureDependencySiteNames.MethodReturn, ArchitectureDependencySiteNames.New));

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("allowedSites=\"MethodReturn, New\"");
	}

	private sealed class TemporaryDirectory : IDisposable
	{
		private readonly string path = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphEditingTests", Guid.NewGuid().ToString("N"));

		public string WriteFile(string fileName, string content, Encoding? encoding = null)
		{
			Directory.CreateDirectory(path);
			var filePath = Path.Combine(path, fileName);
			File.WriteAllText(filePath, content, encoding ?? Encoding.UTF8);

			return filePath;
		}

		public string GetPath(string fileName)
		{
			Directory.CreateDirectory(path);
			var result = Path.Combine(path, fileName);

			return result;
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
