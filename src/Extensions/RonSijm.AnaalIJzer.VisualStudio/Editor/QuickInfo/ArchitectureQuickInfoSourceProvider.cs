using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using RonSijm.AnaalIJzer.VisualStudio.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio;

[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name("AnaalIJzer QuickInfo")]
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
