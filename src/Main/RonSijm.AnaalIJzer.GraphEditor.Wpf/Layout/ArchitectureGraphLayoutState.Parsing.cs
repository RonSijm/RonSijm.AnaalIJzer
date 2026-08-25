using System.Globalization;
using System.Windows;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;

public sealed partial class ArchitectureGraphLayoutState
{
	private static GraphItemLayout? ParseItem(XElement element)
	{
		var path = element.Attribute("path")?.Value;
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		var item = new GraphItemLayout(path!);
		if (TryReadDouble(element, "x", out var x) && TryReadDouble(element, "y", out var y))
		{
			item.Location = new Point(x, y);
		}

		if (TryReadDouble(element, "width", out var width) && TryReadDouble(element, "height", out var height))
		{
			item.Size = new Size(width, height);
		}

		return item;
	}

	private static GraphGroupLayout? ParseGroup(XElement element)
	{
		var key = element.Attribute("key")?.Value;
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}

		var group = new GraphGroupLayout(key!);
		if (TryReadDouble(element, "height", out var height) && IsUsableDimension(height))
		{
			group.Height = height;
		}

		if (TryReadBoolean(element, "collapsed", out var isCollapsed))
		{
			group.IsCollapsed = isCollapsed;
		}

		return group;
	}

	private static bool TryReadDouble(XElement element, string attributeName, out double value)
	{
		var text = element.Attribute(attributeName)?.Value;
		var result = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

		return result;
	}

	private static bool TryReadBoolean(XElement element, string attributeName, out bool value)
	{
		var text = element.Attribute(attributeName)?.Value;
		var result = bool.TryParse(text, out value);

		return result;
	}

	private static bool IsUsableDimension(double value)
	{
		var result = value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);

		return result;
	}

	private sealed class GraphItemLayout(string path)
	{
		public string Path { get; } = path;

		public Point? Location { get; set; }

		public Size? Size { get; set; }
	}

	private sealed class GraphGroupLayout(string key)
	{
		public string Key { get; } = key;

		public double? Height { get; set; }

		public bool? IsCollapsed { get; set; }
	}
}
