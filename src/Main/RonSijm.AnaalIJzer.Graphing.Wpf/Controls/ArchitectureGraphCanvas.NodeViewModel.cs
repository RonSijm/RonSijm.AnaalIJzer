using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifyGraphNodeViewModel : INotifyPropertyChanged
	{
		private readonly ArchitectureGraphCanvasTheme theme;
		private readonly Action<ArchitectureConfigurationEditResult, bool>? editResultHandler;
		private readonly Func<string, bool>? confirmationHandler;
		private readonly Func<ArchitectureLayerCreationRequest?>? layerCreationHandler;
		private readonly ArchitectureGraphLayoutState layoutState;
		private NodifyGraphBoundaryViewModel? containingBoundary;
		private Point location;

		private NodifyGraphNodeViewModel(
			ArchitectureGraphNodeViewModel node,
			Brush headerBrush,
			Brush contentBrush,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			this.theme = theme;
			this.editResultHandler = editResultHandler;
			this.confirmationHandler = confirmationHandler;
			this.layerCreationHandler = layerCreationHandler;
			this.layoutState = layoutState;
			Path = node.Path;
			DisplayName = node.DisplayName;
			Description = node.Description;
			IsActive = node.IsActive;
			TypeCount = node.TypeCount;
			IncomingViolationCount = node.IncomingViolationCount;
			OutgoingViolationCount = node.OutgoingViolationCount;
			EditHandle = node.EditHandle;
			HeaderBrush = headerBrush;
			ContentBrush = contentBrush;
			location = layoutState.GetLocation(node.Path, new Point(node.X, node.Y));
			Input = new NodifyGraphConnectorViewModel(Path, "in", false);
			Output = new NodifyGraphConnectorViewModel(Path, "out", true);
			Inputs = ImmutableArray.Create(Input);
			Outputs = ImmutableArray.Create(Output);
			AddChildLayerCommand = new DelegateCommand(_ => AddChildLayer(), _ => EditHandle.CanEdit);
			RemoveCommand = new DelegateCommand(_ => Remove(), _ => EditHandle.CanEdit);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public string Path { get; }

		public string DisplayName { get; }

		public string? Description { get; }

		public bool IsActive { get; }

		public int TypeCount { get; }

		public int IncomingViolationCount { get; }

		public int OutgoingViolationCount { get; }

		public ArchitectureLayerEditHandle EditHandle { get; }

		public Brush HeaderBrush { get; }

		public Brush ContentBrush { get; }

		public Brush Foreground
		{
			get
			{
				var result = theme.NodeForeground;

				return result;
			}
		}

		public NodifyGraphConnectorViewModel Input { get; }

		public NodifyGraphConnectorViewModel Output { get; }

		public ImmutableArray<NodifyGraphConnectorViewModel> Inputs { get; }

		public ImmutableArray<NodifyGraphConnectorViewModel> Outputs { get; }

		public ICommand RemoveCommand { get; }

		public ICommand AddChildLayerCommand { get; }

		public Point Location
		{
			get { return location; }
			set { SetLocation(value, true); }
		}

		public Brush BorderBrush
		{
			get
			{
				var result = IsActive ? theme.ActiveConnection : theme.Border;

				return result;
			}
		}

		public Thickness BorderThickness
		{
			get
			{
				var result = new Thickness(IsActive ? 3 : 1);

				return result;
			}
		}

		public string ToolTip
		{
			get
			{
				var description = string.IsNullOrWhiteSpace(Description) ? string.Empty : Environment.NewLine + Description;
				var evidence = TypeCount > 0
					? Environment.NewLine + TypeCount + " matching type" + (TypeCount == 1 ? string.Empty : "s") + ". Violations: " + (IncomingViolationCount + OutgoingViolationCount)
					: string.Empty;
				var result = Path + description + evidence + Environment.NewLine + "Drag to rearrange this graph.";

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
				var result = Path + evidence + violationText;

				return result;
			}
		}

		public static NodifyGraphNodeViewModel Create(
			ArchitectureGraphNodeViewModel node,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			var headerBrush = node.PaletteSlot <= 0 ? ArchitectureGraphPalette.GetUnclassifiedBrush() : ArchitectureGraphPalette.GetBrush(node.PaletteSlot);
			var contentBrush = CreateContentBrush(headerBrush);
			var result = new NodifyGraphNodeViewModel(node, headerBrush, contentBrush, editResultHandler, confirmationHandler, layerCreationHandler, layoutState, theme);

			return result;
		}

		public void Attach(NodifyGraphBoundaryViewModel? boundary)
		{
			containingBoundary = boundary;
		}

		public void MoveBy(Vector delta, bool constrain)
		{
			SetLocation(Location + delta, constrain);
		}

		private void AddChildLayer()
		{
			var request = layerCreationHandler?.Invoke();
			if (request is null)
			{
				return;
			}

			var source = new ArchitectureConfigurationSource(EditHandle.SourceKind, EditHandle.SourcePath);
			var result = ArchitectureConfigurationEditService.AddLayer(source, Path, request.Name, request.MatcherKind, request.MatcherAttributes);
			editResultHandler?.Invoke(result, false);
		}

		private void Remove()
		{
			if (confirmationHandler is not null && !confirmationHandler("Remove layer '" + Path + "' and its nested settings?"))
			{
				return;
			}

			var result = ArchitectureConfigurationEditService.RemoveLayer(EditHandle);
			editResultHandler?.Invoke(result, true);
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

		private void SetLocation(Point value, bool constrain)
		{
			var nextLocation = constrain ? CoerceLocation(value) : value;
			if (location == nextLocation)
			{
				return;
			}

			location = nextLocation;
			layoutState.SetLocation(Path, location);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
			containingBoundary?.RefreshMinimumSize();
		}

		private Point CoerceLocation(Point value)
		{
			if (containingBoundary is null)
			{
				return value;
			}

			var minX = containingBoundary.Location.X + BoundaryPaddingX;
			var minY = containingBoundary.Location.Y + BoundaryPaddingTop;
			var maxX = containingBoundary.Location.X + Math.Max(BoundaryPaddingX, containingBoundary.Width - NodeWidth - BoundaryPaddingX);
			var maxY = containingBoundary.Location.Y + Math.Max(BoundaryPaddingTop, containingBoundary.Height - NodeHeight - BoundaryPaddingBottom);
			var result = new Point(Clamp(value.X, minX, maxX), Clamp(value.Y, minY, maxY));

			return result;
		}
	}
}
