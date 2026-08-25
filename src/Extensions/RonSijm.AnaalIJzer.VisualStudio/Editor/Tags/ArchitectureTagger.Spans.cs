using Microsoft.VisualStudio.Text;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Tags;

internal sealed partial class ArchitectureTagger
{
	private static bool TryCreatePointSpan(ITextSnapshot snapshot, int position, out SnapshotSpan span)
	{
		if (position < 0 || position > snapshot.Length)
		{
			span = default;
			return false;
		}

		span = new SnapshotSpan(snapshot, position, 0);
		return true;
	}

	private static bool TryCreateSourceSpan(ITextSnapshot snapshot, Microsoft.CodeAnalysis.Text.TextSpan sourceSpan, out SnapshotSpan span)
	{
		if (sourceSpan.Start < 0 || sourceSpan.End > snapshot.Length)
		{
			span = default;
			return false;
		}

		span = new SnapshotSpan(snapshot, sourceSpan.Start, sourceSpan.Length);
		return true;
	}

	private static bool TryCreateFullLineSourceSpan(ITextSnapshot snapshot, Microsoft.CodeAnalysis.Text.TextSpan sourceSpan, out SnapshotSpan span)
	{
		if (sourceSpan.Start < 0 || sourceSpan.End > snapshot.Length)
		{
			span = default;
			return false;
		}

		var startLine = snapshot.GetLineFromPosition(sourceSpan.Start);
		var endPosition = Math.Max(sourceSpan.Start, sourceSpan.End - 1);
		var endLine = snapshot.GetLineFromPosition(endPosition);
		span = new SnapshotSpan(snapshot, startLine.Start, endLine.EndIncludingLineBreak);
		return true;
	}
}
