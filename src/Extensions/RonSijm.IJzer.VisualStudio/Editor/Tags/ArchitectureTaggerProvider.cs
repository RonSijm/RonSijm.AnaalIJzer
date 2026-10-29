using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using RonSijm.IJzer.VisualStudio.Diagnostics;
using RonSijm.IJzer.VisualStudio.Editor.LayerIndicators;
using RonSijm.IJzer.VisualStudio.Editor.Snapshots;

namespace RonSijm.IJzer.VisualStudio.Editor.Tags;

[Export(typeof(IViewTaggerProvider))]
[Name("IJzer Architecture Tags")]
[ContentType("CSharp")]
[TagType(typeof(IntraTextAdornmentTag))]
[TagType(typeof(InterLineAdornmentTag))]
[TagType(typeof(TextMarkerTag))]
[TagType(typeof(ArchitectureLayerGlyphTag))]
internal sealed class ArchitectureTaggerProvider : IViewTaggerProvider
{
    private readonly ArchitectureSnapshotProvider _snapshotProvider;

    [ImportingConstructor]
    public ArchitectureTaggerProvider(ArchitectureSnapshotProvider snapshotProvider)
    {
        this._snapshotProvider = snapshotProvider;
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
        var tagger = textView.Properties.GetOrCreateSingletonProperty(() => new ArchitectureTagger(textView, buffer, _snapshotProvider));
        var result = tagger as ITagger<T>;

        return result;
    }
}