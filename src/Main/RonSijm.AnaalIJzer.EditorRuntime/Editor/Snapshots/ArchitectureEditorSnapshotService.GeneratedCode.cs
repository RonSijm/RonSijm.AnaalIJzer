using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Observations;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static bool IsGenerated(Document document, SyntaxNode syntaxRoot, CancellationToken cancellationToken)
	{
		var result = GeneratedCodeDetector.IsGenerated(syntaxRoot.SyntaxTree, cancellationToken);

		return result;
	}

	private readonly struct CallerInfo(string typeName, string layerPath, LayerMatch match)
    {
        public string TypeName { get; } = typeName;

        public string LayerPath { get; } = layerPath;

        public LayerMatch Match { get; } = match;
    }
}
