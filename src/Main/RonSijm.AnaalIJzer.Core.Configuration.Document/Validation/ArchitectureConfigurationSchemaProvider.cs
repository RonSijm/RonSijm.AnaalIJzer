using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static class ArchitectureConfigurationSchemaProvider
{
	private static readonly Lazy<XmlSchemaSet> CachedSchemas = new(CreateSchemas);

	public static XmlSchemaSet Schemas
	{
		get
		{
			var result = CachedSchemas.Value;

			return result;
		}
	}

	private static XmlSchemaSet CreateSchemas()
	{
		var assembly = typeof(ArchitectureConfigurationSchemaProvider).GetTypeInfo().Assembly;
		using var stream = assembly.GetManifestResourceStream("RonSijm.AnaalIJzer.AnaalIJzer.xsd")
			?? throw new InvalidOperationException("Embedded AnaalIJzer.xsd schema was not found.");
		using var reader = XmlReader.Create(stream);
		var schemas = new XmlSchemaSet();
		schemas.Add(string.Empty, reader);
		schemas.Compile();

		return schemas;
	}
}
