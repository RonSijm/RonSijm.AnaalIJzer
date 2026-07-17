using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationXmlSerializer
{
	internal static string SerializeXml(XDocument document)
	{
		var settings = new XmlWriterSettings
		{
			Indent = true,
			OmitXmlDeclaration = document.Declaration is null,
			Encoding = new UTF8Encoding(false)
		};

		using var stream = new MemoryStream();
		using (var writer = XmlWriter.Create(stream, settings))
		{
			document.Save(writer);
		}

		var result = Encoding.UTF8.GetString(stream.ToArray());

		return result;
	}
}
