using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
	private sealed partial class NodifyGraphBoundaryViewModel : INotifyPropertyChanged
	{
		private readonly IArchitectureGraphEditService editService;
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
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			this.editService = editService;
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
			ExceptionReviewCount = boundary.ExceptionReviewCount;
			ExceptionReviewSummaries = boundary.ExceptionReviewSummaries;
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

		public int ExceptionReviewCount { get; }

		public ImmutableArray<string> ExceptionReviewSummaries { get; }

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
			var result = editService.AddLayer(source, Path, request.Name, request.MatcherKind, request.MatcherAttributes);
			editResultHandler?.Invoke(result, false);
		}

		private void Remove()
		{
			if (confirmationHandler is not null && !confirmationHandler("Remove layer '" + Path + "' and its nested settings?"))
			{
				return;
			}

			var result = editService.RemoveLayer(EditHandle);
			editResultHandler?.Invoke(result, true);
		}
	}
}
