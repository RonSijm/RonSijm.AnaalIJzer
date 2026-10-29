using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using RonSijm.IJzer.VisualStudio.Editor.Snapshots;

namespace RonSijm.IJzer.VisualStudio.Editor.QuickInfo;

[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name("IJzer QuickInfo")]
[ContentType("CSharp")]
[method: ImportingConstructor]
internal sealed class ArchitectureQuickInfoSourceProvider(ArchitectureSnapshotProvider snapshotProvider)
    : IAsyncQuickInfoSourceProvider
{
    public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
    {
        var result = textBuffer.Properties.GetOrCreateSingletonProperty(() => new ArchitectureQuickInfoSource(textBuffer, snapshotProvider));

        return result;
    }
}