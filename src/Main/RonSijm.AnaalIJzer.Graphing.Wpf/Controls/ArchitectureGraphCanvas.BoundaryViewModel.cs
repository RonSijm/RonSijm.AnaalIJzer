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
	private sealed class NodifyGraphBoundaryViewModel : INotifyPropertyChanged
	{
		private readonly Action<ArchitectureConfigurationEditResult, bool>? editResultHandler;
		private readonly Func<string, bool>? confirmationHandler;
		private readonly Func<ArchitectureLayerCreationRequest?>? layerCreationHandler;
		private readonly ArchitectureGraphLayoutState layoutState;
		private ImmutableArray<NodifyGraphNodeViewModel> directNodes = ImmutableArray<NodifyGraphNodeViewModel>.Empty;
		private ImmutableArray<NodifyGraphBoundaryViewModel> directBoundaries = ImmutableArray<NodifyGraphBoundaryViewModel>.Empty;
		private NodifyGraphBoundaryViewModel? parentBoundary;
		private Point location;
		private Size actualSize;

		private NodifyGraphBoundaryViewModel(
			ArchitectureGraphBoundaryViewModel boundary,
			Brush background,
			Brush borderBrush,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			this.editResultHandler = editResultHandler;
			this.confirmationHandler = confirmationHandler;
			this.layerCreationHandler = layerCreationHandler;
			this.layoutState = layoutState;
			Path = boundary.Path;
			DisplayName = boundary.DisplayName;
			Description = boundary.Description;
			IsActive = boundary.IsActive;
			TypeCount = boundary.TypeCount;
			IncomingViolationCount = boundary.IncomingViolationCount;
			OutgoingViolationCount = boundary.OutgoingViolationCount;
			EditHandle = boundary.EditHandle;
			location = layoutState.GetLocation(boundary.Path, new Point(boundary.X, boundary.Y));
			actualSize = layoutState.GetSize(boundary.Path, new Size(boundary.Width, boundary.Height));
			Background = background;
			BorderBrush = borderBrush;
			HeaderBrush = borderBrush;
			Foreground = theme.Foreground;
			Input = new NodifyGraphConnectorViewModel(Path, "in", false);
			Output = new NodifyGraphConnectorViewModel(Path, "out", true);
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

		public NodifyGraphConnectorViewModel Input { get; }

		public NodifyGraphConnectorViewModel Output { get; }

		public ICommand RemoveCommand { get; }

		public ICommand AddChildLayerCommand { get; }

		public Point Location
		{
			get { return location; }
			set { SetLocation(value, true, true); }
		}

		public Size ActualSize
		{
			get { return actualSize; }
			set
			{
				var nextSize = CoerceSize(value);
				if (actualSize == nextSize)
				{
					return;
				}

				actualSize = nextSize;
				NotifySizeChanged();
			}
		}

		public double Width
		{
			get { return ActualSize.Width; }
		}

		public double Height
		{
			get { return ActualSize.Height; }
		}

		public double MinimumWidth
		{
			get { return CalculateMinimumSize().Width; }
		}

		public double MinimumHeight
		{
			get { return CalculateMinimumSize().Height; }
		}

		public Brush Background { get; }

		public Brush HeaderBrush { get; }

		public Brush BorderBrush { get; }

		public Brush Foreground { get; }

		public Thickness BorderThickness
		{
			get
			{
				var result = new Thickness(IsActive ? 2.5 : 1.2);

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
				var result = Path + description + evidence + Environment.NewLine + "Nested layer boundary.";

				return result;
			}
		}

		public string HeaderText
		{
			get
			{
				var evidence = TypeCount > 0 ? "  (" + TypeCount + ")" : string.Empty;
				var result = DisplayName + evidence;

				return result;
			}
		}

		public static NodifyGraphBoundaryViewModel Create(
			ArchitectureGraphBoundaryViewModel boundary,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			var paletteBrush = boundary.PaletteSlot <= 0 ? theme.Foreground : ArchitectureGraphPalette.GetBrush(boundary.PaletteSlot);
			var background = CreateOpacityBrush(paletteBrush, 0.10);
			var border = CreateOpacityBrush(paletteBrush, boundary.IsActive ? 0.85 : 0.48);
			var result = new NodifyGraphBoundaryViewModel(boundary, background, border, editResultHandler, confirmationHandler, layerCreationHandler, layoutState, theme);

			return result;
		}

		public void Attach(NodifyGraphBoundaryViewModel? parent, ImmutableArray<NodifyGraphNodeViewModel> nodes, ImmutableArray<NodifyGraphBoundaryViewModel> boundaries)
		{
			parentBoundary = parent;
			directNodes = nodes;
			directBoundaries = boundaries;
			ActualSize = actualSize;
			NotifyMinimumSizeChanged();
		}

		public void MoveBy(Vector delta, bool constrain)
		{
			SetLocation(Location + delta, constrain, true);
		}

		public void RefreshMinimumSize()
		{
			var minimumSize = CalculateMinimumSize();
			if (actualSize.Width < minimumSize.Width || actualSize.Height < minimumSize.Height)
			{
				ActualSize = actualSize;
				return;
			}

			NotifyMinimumSizeChanged();
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

		private void SetLocation(Point value, bool constrain, bool moveContents)
		{
			var nextLocation = constrain ? CoerceLocation(value) : value;
			if (location == nextLocation)
			{
				return;
			}

			var delta = nextLocation - location;
			location = nextLocation;
			layoutState.SetLocation(Path, location);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
			if (moveContents)
			{
				MoveContents(delta);
			}

			parentBoundary?.RefreshMinimumSize();
		}

		private Point CoerceLocation(Point value)
		{
			if (parentBoundary is null)
			{
				return value;
			}

			var minX = parentBoundary.Location.X + BoundaryPaddingX;
			var minY = parentBoundary.Location.Y + BoundaryPaddingTop;
			var maxX = parentBoundary.Location.X + Math.Max(BoundaryPaddingX, parentBoundary.Width - Width - BoundaryPaddingX);
			var maxY = parentBoundary.Location.Y + Math.Max(BoundaryPaddingTop, parentBoundary.Height - Height - BoundaryPaddingBottom);
			var result = new Point(Clamp(value.X, minX, maxX), Clamp(value.Y, minY, maxY));

			return result;
		}

		private void MoveContents(Vector delta)
		{
			foreach (var boundary in directBoundaries)
			{
				boundary.MoveBy(delta, false);
			}

			foreach (var node in directNodes)
			{
				node.MoveBy(delta, false);
			}
		}

		private Size CoerceSize(Size value)
		{
			var minimumSize = CalculateMinimumSize();
			var result = new Size(
				Math.Max(minimumSize.Width, value.Width),
				Math.Max(minimumSize.Height, value.Height));

			return result;
		}

		private Size CalculateMinimumSize()
		{
			var minimumWidth = NodeWidth + BoundaryPaddingX * 2;
			var minimumHeight = NodeHeight + BoundaryPaddingTop + BoundaryPaddingBottom;
			foreach (var node in directNodes)
			{
				minimumWidth = Math.Max(minimumWidth, node.Location.X - Location.X + NodeWidth + BoundaryPaddingX);
				minimumHeight = Math.Max(minimumHeight, node.Location.Y - Location.Y + NodeHeight + BoundaryPaddingBottom);
			}

			foreach (var boundary in directBoundaries)
			{
				minimumWidth = Math.Max(minimumWidth, boundary.Location.X - Location.X + boundary.Width + BoundaryPaddingX);
				minimumHeight = Math.Max(minimumHeight, boundary.Location.Y - Location.Y + boundary.Height + BoundaryPaddingBottom);
			}

			var result = new Size(minimumWidth, minimumHeight);

			return result;
		}

		private void NotifySizeChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualSize)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
			layoutState.SetSize(Path, actualSize);
			NotifyMinimumSizeChanged();
			parentBoundary?.RefreshMinimumSize();
		}

		private void NotifyMinimumSizeChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimumWidth)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimumHeight)));
		}
	}
}
