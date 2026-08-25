using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;

public sealed partial class ArchitectureGraphLayoutState
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

	public static string CreateSourceKey(ArchitectureConfigurationSource source)
	{
		var normalizedPath = NormalizePath(source.Path);
		var result = source.Kind + "|" + normalizedPath;

		return result;
	}
}
