using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using RonSijm.IJzer.Core.Editor.Snapshots;
using RonSijm.IJzer.VisualStudio.Graphs;

namespace RonSijm.IJzer.VisualStudio.Editor.Snapshots;

internal sealed partial class ArchitectureSnapshotProvider
{
    internal ArchitectureGraphToolWindowContext CreateGraphToolWindowContext(ITextBuffer buffer, ArchitectureEditorSnapshot snapshot)
    {
        if (!_textDocumentFactory.TryGetTextDocument(buffer, out var textDocument))
        {
            var noTextDocumentResult = new ArchitectureGraphToolWindowContext(snapshot.GraphSnapshot, null, null, null);

            return noTextDocumentResult;
        }

        var documentId = FindDocumentId(textDocument.FilePath);
        if (documentId is null)
        {
            var noDocumentIdResult = new ArchitectureGraphToolWindowContext(snapshot.GraphSnapshot, textDocument.FilePath, null, null);

            return noDocumentIdResult;
        }

        var document = _workspace.CurrentSolution.GetDocument(documentId);
        if (document is null)
        {
            var noDocumentResult = new ArchitectureGraphToolWindowContext(snapshot.GraphSnapshot, textDocument.FilePath, null, null);

            return noDocumentResult;
        }

        var result = new ArchitectureGraphToolWindowContext(
            snapshot.GraphSnapshot,
            textDocument.FilePath,
            document.Project.FilePath,
            document.Project.Solution.FilePath);

        return result;
    }
}