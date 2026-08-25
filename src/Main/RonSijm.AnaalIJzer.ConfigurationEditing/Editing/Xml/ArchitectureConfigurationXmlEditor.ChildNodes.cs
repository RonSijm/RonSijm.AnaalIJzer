using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlEditor
{
	internal static bool TryParseChildNodes(string childXml, out ImmutableArray<XNode> childNodes, out string message)
	{
		if (string.IsNullOrWhiteSpace(childXml))
		{
			childNodes = ImmutableArray<XNode>.Empty;
			message = string.Empty;
			return true;
		}

		try
		{
			var wrapper = XElement.Parse("<AnaalIJzerChildren>" + childXml + "</AnaalIJzerChildren>", LoadOptions.PreserveWhitespace);
			childNodes = wrapper.Nodes().Select(CloneNode).ToImmutableArray();
			message = string.Empty;
			return true;
		}
		catch (XmlException exception)
		{
			childNodes = ImmutableArray<XNode>.Empty;
			message = "Child XML is invalid: " + exception.Message;
			return false;
		}
	}

	internal static XNode CloneNode(XNode node)
	{
		XNode result = node switch
		{
			XElement element => new XElement(element),
			XText text => new XText(text.Value),
			XComment comment => new XComment(comment.Value),
			_ => new XText(node.ToString())
		};

		return result;
	}
}
