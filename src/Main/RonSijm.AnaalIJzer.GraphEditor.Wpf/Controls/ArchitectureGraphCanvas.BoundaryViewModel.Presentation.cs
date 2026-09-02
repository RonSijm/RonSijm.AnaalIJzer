using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphBoundaryViewModel
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
				var result = Path + description + evidence + exceptionReviews + Environment.NewLine + "Nested layer boundary.";

				return result;
			}
		}

		public string HeaderText
		{
			get
			{
				var evidence = TypeCount > 0 ? "  (" + TypeCount + ")" : string.Empty;
				var exceptionReviews = ExceptionReviewCount > 0 ? "  [ARCH017 " + ExceptionReviewCount + "]" : string.Empty;
				var result = DisplayName + evidence + exceptionReviews;

				return result;
			}
		}

		public static NodifyGraphBoundaryViewModel Create(
			ArchitectureGraphBoundaryViewModel boundary,
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Action<ArchitectureGraphSelection>? selectionHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			var paletteBrush = boundary.PaletteSlot <= 0 ? theme.Foreground : ArchitectureGraphPalette.GetBrush(boundary.PaletteSlot);
			var background = CreateOpacityBrush(paletteBrush, 0.10);
			var border = CreateOpacityBrush(paletteBrush, boundary.IsActive ? 0.85 : 0.48);
			var result = new NodifyGraphBoundaryViewModel(boundary, background, border, editService, editResultHandler, selectionHandler, confirmationHandler, layerCreationHandler, layoutState, theme);

			return result;
		}

		private static Brush CreateOpacityBrush(Brush source, double opacity)
		{
			if (source is not SolidColorBrush solid)
			{
				return source;
			}

			var result = new SolidColorBrush(Color.FromArgb((byte)Math.Round(byte.MaxValue * opacity), solid.Color.R, solid.Color.G, solid.Color.B));
			result.Freeze();

			return result;
		}
	}
}
