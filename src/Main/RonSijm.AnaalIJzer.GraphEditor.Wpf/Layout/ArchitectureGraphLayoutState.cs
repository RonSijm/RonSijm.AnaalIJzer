using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;

public sealed partial class ArchitectureGraphLayoutState
{
	private readonly Dictionary<string, GraphItemLayout> _items;
	private readonly Dictionary<string, GraphGroupLayout> _groups;
	private readonly Action<string>? _warningLogger;
	private bool _isDirty;

	private ArchitectureGraphLayoutState(string sourceKey, string? userSettingsPath, Dictionary<string, GraphItemLayout> items, Dictionary<string, GraphGroupLayout> groups, Action<string>? warningLogger)
	{
		SourceKey = sourceKey;
		UserSettingsPath = userSettingsPath;
		this._items = items;
		this._groups = groups;
		this._warningLogger = warningLogger;
	}

	public string SourceKey { get; }

	public string? UserSettingsPath { get; }

	public static string CreateSourceKey(ArchitectureConfigurationSource source)
	{
		var normalizedPath = NormalizePath(source.Path);
		var result = source.Kind + "|" + normalizedPath;

		return result;
	}
}
