using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphNodeViewModel
	{
		public string ToolTip
		{
			get
			{
				var description = string.IsNullOrWhiteSpace(Description) ? string.Empty : Environment.NewLine + Description;
				var evidence = TypeCount > 0
					? Environment.NewLine + TypeCount + " matching type" + (TypeCount == 1 ? string.Empty : "s") + ". Violations: " + (IncomingViolationCount + OutgoingViolationCount)
					: string.Empty;
				var exceptionReviews = ExceptionReviewCount > 0
					? Environment.NewLine + "Exception reviews: " + ExceptionReviewCount + Environment.NewLine + string.Join(Environment.NewLine, ExceptionReviewSummaries)
					: string.Empty;
				var result = Path + description + evidence + exceptionReviews + Environment.NewLine + "Drag to rearrange this graph.";

				return result;
			}
		}

		public string ContentText
		{
			get
			{
				var evidence = TypeCount > 0
					? Environment.NewLine + TypeCount + " type" + (TypeCount == 1 ? string.Empty : "s")
					: string.Empty;
				var violations = IncomingViolationCount + OutgoingViolationCount;
				var violationText = violations > 0 ? Environment.NewLine + violations + " violation" + (violations == 1 ? string.Empty : "s") : string.Empty;
				var exceptionReviews = ExceptionReviewCount > 0 ? Environment.NewLine + ExceptionReviewCount + " exception review" + (ExceptionReviewCount == 1 ? string.Empty : "s") : string.Empty;
				var result = Path + evidence + violationText + exceptionReviews;

				return result;
			}
		}

		public static NodifyGraphNodeViewModel Create(
			ArchitectureGraphNodeViewModel node,
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			var headerBrush = node.PaletteSlot <= 0 ? ArchitectureGraphPalette.GetUnclassifiedBrush() : ArchitectureGraphPalette.GetBrush(node.PaletteSlot);
			var contentBrush = CreateContentBrush(headerBrush);
			var result = new NodifyGraphNodeViewModel(node, headerBrush, contentBrush, editService, editResultHandler, confirmationHandler, layerCreationHandler, layoutState, theme);

			return result;
		}

		private static Brush CreateContentBrush(Brush headerBrush)
		{
			if (headerBrush is not SolidColorBrush solid)
			{
				return headerBrush;
			}

			var color = solid.Color;
			var result = new SolidColorBrush(Color.FromRgb(
				(byte)Math.Max(0, color.R - 28),
				(byte)Math.Max(0, color.G - 28),
				(byte)Math.Max(0, color.B - 28)));
			result.Freeze();

			return result;
		}
	}
}
