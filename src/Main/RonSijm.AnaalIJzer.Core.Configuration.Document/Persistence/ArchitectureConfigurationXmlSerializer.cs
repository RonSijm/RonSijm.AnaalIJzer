using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

public static class ArchitectureConfigurationXmlSerializer
{
	public static string SerializeXml(XDocument document)
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
