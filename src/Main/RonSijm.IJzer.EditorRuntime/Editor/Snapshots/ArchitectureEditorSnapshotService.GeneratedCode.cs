using RonSijm.IJzer.Core.LayerModel;

namespace RonSijm.IJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
    private readonly struct CallerInfo(string typeName, string layerPath, LayerMatch match)
    {
        public string TypeName { get; } = typeName;

        public string LayerPath { get; } = layerPath;

        public LayerMatch Match { get; } = match;
    }
}