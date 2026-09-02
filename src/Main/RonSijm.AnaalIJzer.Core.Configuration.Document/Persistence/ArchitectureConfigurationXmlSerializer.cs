using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

public static class ArchitectureConfigurationXmlSerializer
{
	public static string SerializeXml(XDocument document)
	{
		var normalizedDocument = CreateNormalizedDocument(document);
		var settings = new XmlWriterSettings
		{
			Indent = true,
			OmitXmlDeclaration = normalizedDocument.Declaration is null,
			Encoding = new UTF8Encoding(false)
		};

		using var stream = new MemoryStream();
		using (var writer = XmlWriter.Create(stream, settings))
		{
			normalizedDocument.Save(writer);
		}

		var result = Encoding.UTF8.GetString(stream.ToArray());

		return result;
	}

	private static XDocument CreateNormalizedDocument(XDocument document)
	{
		if (document.Root is null)
		{
			return document;
		}

		var declaration = document.Declaration is null
			? null
			: new XDeclaration(document.Declaration.Version, document.Declaration.Encoding, document.Declaration.Standalone);
		var root = XElement.Parse(document.Root.ToString(SaveOptions.DisableFormatting));
		var result = new XDocument(declaration, root);

		return result;
	}
}
