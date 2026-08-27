using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphBoundaryViewModel : INotifyPropertyChanged
	{
		private readonly IArchitectureGraphEditService _editService;
		private readonly Action<ArchitectureConfigurationEditResult, bool>? _editResultHandler;
		private readonly Func<string, bool>? _confirmationHandler;
		private readonly Func<ArchitectureLayerCreationRequest?>? _layerCreationHandler;
		private readonly ArchitectureGraphLayoutState _layoutState;
		private ImmutableArray<NodifyGraphNodeViewModel> _directNodes = ImmutableArray<NodifyGraphNodeViewModel>.Empty;
		private ImmutableArray<NodifyGraphBoundaryViewModel> _directBoundaries = ImmutableArray<NodifyGraphBoundaryViewModel>.Empty;
		private NodifyGraphBoundaryViewModel? _parentBoundary;
		private Point _location;
		private Size _actualSize;

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
			this._editService = editService;
			this._editResultHandler = editResultHandler;
			this._confirmationHandler = confirmationHandler;
			this._layerCreationHandler = layerCreationHandler;
			this._layoutState = layoutState;
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
			_location = layoutState.GetLocation(boundary.Path, new Point(boundary.X, boundary.Y));
			_actualSize = layoutState.GetSize(boundary.Path, new Size(boundary.Width, boundary.Height));
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
			get => _location;
			set => SetLocation(value, true, true);
		}

		public Size ActualSize
		{
			get { return _actualSize; }
			set
			{
				var nextSize = CoerceSize(value);
				if (_actualSize == nextSize)
				{
					return;
				}

				_actualSize = nextSize;
				NotifySizeChanged();
			}
		}

		public double Width => ActualSize.Width;

		public double Height => ActualSize.Height;

		public double MinimumWidth => CalculateMinimumSize().Width;

		public double MinimumHeight => CalculateMinimumSize().Height;

		public Brush Background { get; }

		public Brush HeaderBrush { get; }

		public Brush BorderBrush { get; }

		public Brush Foreground { get; }

		public Thickness BorderThickness => new(IsActive ? 2.5 : 1.2);

		public void Attach(NodifyGraphBoundaryViewModel? parent, ImmutableArray<NodifyGraphNodeViewModel> nodes, ImmutableArray<NodifyGraphBoundaryViewModel> boundaries)
		{
			_parentBoundary = parent;
			_directNodes = nodes;
			_directBoundaries = boundaries;
			ActualSize = _actualSize;
			NotifyMinimumSizeChanged();
		}

		public void MoveBy(Vector delta, bool constrain)
		{
			SetLocation(Location + delta, constrain, true);
		}

		public void RefreshMinimumSize()
		{
			var minimumSize = CalculateMinimumSize();
			if (_actualSize.Width < minimumSize.Width || _actualSize.Height < minimumSize.Height)
			{
				ActualSize = _actualSize;
				return;
			}

			NotifyMinimumSizeChanged();
		}

		private void AddChildLayer()
		{
			var request = _layerCreationHandler?.Invoke();
			if (request is null)
			{
				return;
			}

			var source = new ArchitectureConfigurationSource(EditHandle.SourceKind, EditHandle.SourcePath);
			var result = _editService.AddLayer(source, Path, request.Name, request.MatcherKind, request.MatcherAttributes);
			_editResultHandler?.Invoke(result, false);
		}

		private void Remove()
		{
			if (_confirmationHandler is not null && !_confirmationHandler("Remove layer '" + Path + "' and its nested settings?"))
			{
				return;
			}

			var result = _editService.RemoveLayer(EditHandle);
			_editResultHandler?.Invoke(result, true);
		}
	}
}
