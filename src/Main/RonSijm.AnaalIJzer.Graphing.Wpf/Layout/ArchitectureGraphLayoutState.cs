using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Layout;

public sealed class ArchitectureGraphLayoutState
{
	private readonly Dictionary<string, GraphItemLayout> items;
	private readonly Dictionary<string, GraphGroupLayout> groups;
	private readonly Action<string>? warningLogger;
	private bool isDirty;

	private ArchitectureGraphLayoutState(string sourceKey, string? userSettingsPath, Dictionary<string, GraphItemLayout> items, Dictionary<string, GraphGroupLayout> groups, Action<string>? warningLogger)
	{
		SourceKey = sourceKey;
		UserSettingsPath = userSettingsPath;
		this.items = items;
		this.groups = groups;
		this.warningLogger = warningLogger;
	}

	public string SourceKey { get; }

	public string? UserSettingsPath { get; }

	public static ArchitectureGraphLayoutState Load(ArchitectureConfigurationSource source, Action<string>? warningLogger = null)
	{
		var sourceKey = CreateSourceKey(source);
		var userSettingsPath = CreateUserSettingsPath(source);
		if (string.IsNullOrWhiteSpace(userSettingsPath) || !File.Exists(userSettingsPath))
		{
			var empty = new ArchitectureGraphLayoutState(
				sourceKey,
				userSettingsPath,
				new Dictionary<string, GraphItemLayout>(StringComparer.Ordinal),
				new Dictionary<string, GraphGroupLayout>(StringComparer.Ordinal),
				warningLogger);

			return empty;
		}

		try
		{
			var document = XDocument.Load(userSettingsPath);
			var items = document.Root?
				            .Element("GraphLayout")?
				            .Elements("Item")
				            .Select(ParseItem)
				            .Where(item => item is not null)
				            .ToDictionary(item => item!.Path, item => item!, StringComparer.Ordinal)
			            ?? new Dictionary<string, GraphItemLayout>(StringComparer.Ordinal);
			var groups = document.Root?
				             .Element("GraphGroups")?
				             .Elements("Group")
				             .Select(ParseGroup)
				             .Where(group => group is not null)
				             .ToDictionary(group => group!.Key, group => group!, StringComparer.Ordinal)
			             ?? new Dictionary<string, GraphGroupLayout>(StringComparer.Ordinal);
			var result = new ArchitectureGraphLayoutState(sourceKey, userSettingsPath, items, groups, warningLogger);

			return result;
		}
		catch (Exception exception)
		{
			warningLogger?.Invoke("Could not load graph layout user settings from " + userSettingsPath + ". " + exception.Message);
			var result = new ArchitectureGraphLayoutState(
				sourceKey,
				userSettingsPath,
				new Dictionary<string, GraphItemLayout>(StringComparer.Ordinal),
				new Dictionary<string, GraphGroupLayout>(StringComparer.Ordinal),
				warningLogger);

			return result;
		}
	}

	public static string CreateSourceKey(ArchitectureConfigurationSource source)
	{
		var normalizedPath = NormalizePath(source.Path);
		var result = source.Kind + "|" + normalizedPath;

		return result;
	}

	public Point GetLocation(string path, Point fallback)
	{
		var result = items.TryGetValue(path, out var item) && item.Location is not null ? item.Location.Value : fallback;

		return result;
	}

	public Size GetSize(string path, Size fallback)
	{
		var result = items.TryGetValue(path, out var item) && item.Size is not null ? item.Size.Value : fallback;

		return result;
	}

	public void SetLocation(string path, Point location)
	{
		var item = GetOrCreate(path);
		if (item.Location == location)
		{
			return;
		}

		item.Location = location;
		isDirty = true;
	}

	public void SetSize(string path, Size size)
	{
		var item = GetOrCreate(path);
		if (item.Size == size)
		{
			return;
		}

		item.Size = size;
		isDirty = true;
	}

	public double GetGroupHeight(string key, double fallback)
	{
		var result = groups.TryGetValue(key, out var group) && group.Height is not null ? group.Height.Value : fallback;

		return result;
	}

	public bool GetGroupIsCollapsed(string key, bool fallback)
	{
		var result = groups.TryGetValue(key, out var group) && group.IsCollapsed is not null ? group.IsCollapsed.Value : fallback;

		return result;
	}

	public void SetGroupHeight(string key, double height)
	{
		if (!IsUsableDimension(height))
		{
			return;
		}

		var group = GetOrCreateGroup(key);
		if (group.Height == height)
		{
			return;
		}

		group.Height = height;
		isDirty = true;
	}

	public void SetGroupIsCollapsed(string key, bool isCollapsed)
	{
		var group = GetOrCreateGroup(key);
		if (group.IsCollapsed == isCollapsed)
		{
			return;
		}

		group.IsCollapsed = isCollapsed;
		isDirty = true;
	}

	public void Save()
	{
		if (!isDirty || string.IsNullOrWhiteSpace(UserSettingsPath))
		{
			return;
		}

		try
		{
			var directory = Path.GetDirectoryName(UserSettingsPath);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var document = new XDocument(
				new XElement(
					"AnaalIJzerGraphUserSettings",
					new XAttribute("version", "1"),
					new XElement(
						"GraphLayout",
						items.Values
							.Where(item => item.Location is not null || item.Size is not null)
							.OrderBy(item => item.Path, StringComparer.Ordinal)
							.Select(CreateElement)),
					new XElement(
						"GraphGroups",
						groups.Values
							.Where(group => group.Height is not null || group.IsCollapsed is not null)
							.OrderBy(group => group.Key, StringComparer.Ordinal)
							.Select(CreateGroupElement))));
			document.Save(UserSettingsPath);
			isDirty = false;
		}
		catch (Exception exception)
		{
			warningLogger?.Invoke("Could not save graph layout user settings to " + UserSettingsPath + ". " + exception.Message);
		}
	}

	private static string? CreateUserSettingsPath(ArchitectureConfigurationSource source)
	{
		if (!source.CanEdit)
		{
			return null;
		}

		var normalizedPath = NormalizePath(source.Path);
		if (string.IsNullOrWhiteSpace(normalizedPath))
		{
			return null;
		}

		var result = normalizedPath + ".usersettings";

		return result;
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		try
		{
			var result = Path.GetFullPath(path);

			return result;
		}
		catch
		{
			return path;
		}
	}

	private static XElement CreateElement(GraphItemLayout item)
	{
		var element = new XElement("Item", new XAttribute("path", item.Path));
		if (item.Location is not null)
		{
			element.SetAttributeValue("x", Format(item.Location.Value.X));
			element.SetAttributeValue("y", Format(item.Location.Value.Y));
		}

		if (item.Size is not null)
		{
			element.SetAttributeValue("width", Format(item.Size.Value.Width));
			element.SetAttributeValue("height", Format(item.Size.Value.Height));
		}

		return element;
	}

	private static XElement CreateGroupElement(GraphGroupLayout group)
	{
		var element = new XElement("Group", new XAttribute("key", group.Key));
		if (group.Height is not null)
		{
			element.SetAttributeValue("height", Format(group.Height.Value));
		}

		if (group.IsCollapsed is not null)
		{
			element.SetAttributeValue("collapsed", group.IsCollapsed.Value ? "true" : "false");
		}

		return element;
	}

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

	private static string Format(double value)
	{
		var result = value.ToString("R", CultureInfo.InvariantCulture);

		return result;
	}

	private GraphItemLayout GetOrCreate(string path)
	{
		if (items.TryGetValue(path, out var item))
		{
			return item;
		}

		var result = new GraphItemLayout(path);
		items.Add(path, result);

		return result;
	}

	private GraphGroupLayout GetOrCreateGroup(string key)
	{
		if (groups.TryGetValue(key, out var group))
		{
			return group;
		}

		var result = new GraphGroupLayout(key);
		groups.Add(key, result);

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
