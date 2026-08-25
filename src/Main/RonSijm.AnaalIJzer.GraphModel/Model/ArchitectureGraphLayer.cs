using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Model;

public sealed class ArchitectureGraphLayer
{
	public ArchitectureGraphLayer(
		string path,
		string displayName,
		string? description,
		int depth,
		int paletteSlot,
		bool isActive,
		string sourcePath = "",
		ArchitectureConfigurationSourceKind sourceKind = ArchitectureConfigurationSourceKind.None,
		int xmlLineNumber = 0)
	{
		Path = path;
		DisplayName = displayName;
		Description = description;
		Depth = depth;
		PaletteSlot = paletteSlot;
		IsActive = isActive;
		SourcePath = sourcePath;
		SourceKind = sourceKind;
		XmlLineNumber = xmlLineNumber;
		EditHandle = new ArchitectureLayerEditHandle(
			SourceKind,
			SourcePath,
			XmlLineNumber,
			Path,
			DisplayName,
			GetParentPath(Path),
			Description);
	}

	public string Path { get; }

	public string DisplayName { get; }

	public string? Description { get; }

	public int Depth { get; }

	public int PaletteSlot { get; }

	public bool IsActive { get; }

	public string SourcePath { get; }

	public ArchitectureConfigurationSourceKind SourceKind { get; }

	public int XmlLineNumber { get; }

	public ArchitectureLayerEditHandle EditHandle { get; }

	private static string GetParentPath(string path)
	{
		var slashIndex = path.LastIndexOf('/');
		var result = slashIndex <= 0 ? string.Empty : path.Substring(0, slashIndex);

		return result;
	}
}
