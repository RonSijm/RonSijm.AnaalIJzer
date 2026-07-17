using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.LayerIndicators;
using RonSijm.AnaalIJzer.VisualStudio.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Tags;

[Export(typeof(IViewTaggerProvider))]
[Name("AnaalIJzer Architecture Tags")]
[ContentType("CSharp")]
[TagType(typeof(IntraTextAdornmentTag))]
[TagType(typeof(InterLineAdornmentTag))]
[TagType(typeof(TextMarkerTag))]
[TagType(typeof(ArchitectureLayerGlyphTag))]
internal sealed class ArchitectureTaggerProvider : IViewTaggerProvider
{
	private readonly ArchitectureSnapshotProvider snapshotProvider;

	[ImportingConstructor]
	public ArchitectureTaggerProvider(ArchitectureSnapshotProvider snapshotProvider)
	{
		this.snapshotProvider = snapshotProvider;
		ArchitectureVisualStudioLog.Info("ArchitectureTaggerProvider created.");
	}

	public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
	{
		if (textView.TextBuffer != buffer)
		{
			ArchitectureVisualStudioLog.Info("ArchitectureTaggerProvider ignored a non-primary buffer.");
			return null;
		}

		ArchitectureVisualStudioLog.Info("ArchitectureTaggerProvider creating tagger for content type '" + buffer.ContentType.TypeName + "'.");
		var tagger = textView.Properties.GetOrCreateSingletonProperty(() => new ArchitectureTagger(textView, buffer, snapshotProvider));
		var result = tagger as ITagger<T>;

		return result;
	}
}
