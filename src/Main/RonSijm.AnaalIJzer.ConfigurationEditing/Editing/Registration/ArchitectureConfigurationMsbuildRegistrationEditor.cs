using System.Text;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Registration;

internal static class ArchitectureConfigurationMsbuildRegistrationEditor
{
	internal static ArchitectureConfigurationEditResult RegisterAdditionalFile(string registrationPath, string configurationPath, bool createWhenMissing)
	{
		if (string.IsNullOrWhiteSpace(registrationPath))
		{
			return ArchitectureConfigurationEditResult.Failure("No MSBuild file was selected for registering Architecture.anl.");
		}

		var fullRegistrationPath = Path.GetFullPath(registrationPath);
		if (!File.Exists(fullRegistrationPath) && !createWhenMissing)
		{
			return ArchitectureConfigurationEditResult.Failure("MSBuild project file does not exist: " + fullRegistrationPath);
		}

		var document = File.Exists(fullRegistrationPath)
			? ArchitectureConfigurationDocumentLoader.LoadXmlFile(fullRegistrationPath)
			: new XDocument(new XElement("Project"));
		if (document.Root is null || !string.Equals(document.Root.Name.LocalName, "Project", StringComparison.Ordinal))
		{
			return ArchitectureConfigurationEditResult.Failure("MSBuild registration file must have a <Project> root: " + fullRegistrationPath);
		}

		if (HasAdditionalFile(document, fullRegistrationPath, configurationPath))
		{
			return ArchitectureConfigurationEditResult.Success("Architecture.anl was already registered in " + fullRegistrationPath + ".");
		}

		var includePath = CreateIncludePath(fullRegistrationPath, configurationPath);
		var ns = document.Root.Name.Namespace;
		var itemGroup = document.Root.Elements(ns + "ItemGroup").FirstOrDefault();
		if (itemGroup is null)
		{
			itemGroup = new XElement(ns + "ItemGroup");
			document.Root.Add(itemGroup);
		}

		itemGroup.Add(new XElement(ns + "AdditionalFiles", new XAttribute("Include", includePath)));
		var directory = Path.GetDirectoryName(fullRegistrationPath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(fullRegistrationPath, ArchitectureConfigurationXmlSerializer.SerializeXml(document), Encoding.UTF8);
		var result = ArchitectureConfigurationEditResult.Success("Registered Architecture.anl in " + fullRegistrationPath + ".");

		return result;
	}

	private static bool HasAdditionalFile(XDocument document, string registrationPath, string configurationPath)
	{
		var registrationDirectory = Path.GetDirectoryName(Path.GetFullPath(registrationPath)) ?? string.Empty;
		var configurationFullPath = Path.GetFullPath(configurationPath);
		foreach (var additionalFile in document.Descendants().Where(element => string.Equals(element.Name.LocalName, "AdditionalFiles", StringComparison.Ordinal)))
		{
			var include = additionalFile.Attribute("Include")?.Value;
			if (string.IsNullOrWhiteSpace(include))
			{
				continue;
			}

			var resolved = Path.GetFullPath(Path.Combine(registrationDirectory, include));
			if (string.Equals(resolved, configurationFullPath, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static string CreateIncludePath(string registrationPath, string configurationPath)
	{
		var registrationDirectory = Path.GetDirectoryName(Path.GetFullPath(registrationPath)) ?? string.Empty;
		var configurationFullPath = Path.GetFullPath(configurationPath);
		var result = MakeRelativePath(registrationDirectory, configurationFullPath);

		return result;
	}

	private static string MakeRelativePath(string baseDirectory, string path)
	{
		var baseUri = new Uri(EnsureTrailingDirectorySeparator(Path.GetFullPath(baseDirectory)));
		var pathUri = new Uri(Path.GetFullPath(path));
		var relative = Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString());
		var result = relative.Replace('/', Path.DirectorySeparatorChar);

		return result;
	}

	private static string EnsureTrailingDirectorySeparator(string path)
	{
		if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
		    || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
		{
			return path;
		}

		var result = path + Path.DirectorySeparatorChar;

		return result;
	}
}
