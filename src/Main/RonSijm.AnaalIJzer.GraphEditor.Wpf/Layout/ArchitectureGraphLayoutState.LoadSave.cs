using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;

public sealed partial class ArchitectureGraphLayoutState
{
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

	private static string Format(double value)
	{
		var result = value.ToString("R", CultureInfo.InvariantCulture);

		return result;
	}
}
