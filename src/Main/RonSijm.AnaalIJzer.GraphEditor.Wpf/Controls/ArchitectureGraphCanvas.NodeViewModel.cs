using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphNodeViewModel : INotifyPropertyChanged
	{
		private readonly ArchitectureGraphCanvasTheme _theme;
		private readonly IArchitectureGraphEditService _editService;
		private readonly Action<ArchitectureConfigurationEditResult, bool>? _editResultHandler;
		private readonly Action<ArchitectureGraphSelection>? _selectionHandler;
		private readonly Func<string, bool>? _confirmationHandler;
		private readonly Func<ArchitectureLayerCreationRequest?>? _layerCreationHandler;
		private readonly ArchitectureGraphLayoutState _layoutState;
		private NodifyGraphBoundaryViewModel? _containingBoundary;
		private Point _location;

		private NodifyGraphNodeViewModel(
			ArchitectureGraphNodeViewModel node,
			Brush headerBrush,
			Brush contentBrush,
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Action<ArchitectureGraphSelection>? selectionHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			this._theme = theme;
			this._editService = editService;
			this._editResultHandler = editResultHandler;
			this._selectionHandler = selectionHandler;
			this._confirmationHandler = confirmationHandler;
			this._layerCreationHandler = layerCreationHandler;
			this._layoutState = layoutState;
			Path = node.Path;
			DisplayName = node.DisplayName;
			Description = node.Description;
			IsActive = node.IsActive;
			TypeCount = node.TypeCount;
			IncomingViolationCount = node.IncomingViolationCount;
			OutgoingViolationCount = node.OutgoingViolationCount;
			ExceptionReviewCount = node.ExceptionReviewCount;
			ExceptionReviewSummaries = node.ExceptionReviewSummaries;
			EditHandle = node.EditHandle;
			HeaderBrush = headerBrush;
			ContentBrush = contentBrush;
			_location = layoutState.GetLocation(node.Path, new Point(node.X, node.Y));
			Input = new NodifyGraphConnectorViewModel(Path, "in", false);
			Output = new NodifyGraphConnectorViewModel(Path, "out", true);
			Inputs = [Input];
			Outputs = [Output];
			AddChildLayerCommand = new DelegateCommand(_ => AddChildLayer(), _ => EditHandle.CanEdit);
			RemoveCommand = new DelegateCommand(_ => Remove(), _ => EditHandle.CanEdit);
			ShowConfigurationFixesCommand = new DelegateCommand(_ => ShowConfigurationFixes(), _ => _selectionHandler is not null);
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

		public Brush HeaderBrush { get; }

		public Brush ContentBrush { get; }

		public Brush Foreground => _theme.NodeForeground;

		public NodifyGraphConnectorViewModel Input { get; }

		public NodifyGraphConnectorViewModel Output { get; }

		public ImmutableArray<NodifyGraphConnectorViewModel> Inputs { get; }

		public ImmutableArray<NodifyGraphConnectorViewModel> Outputs { get; }

		public ICommand RemoveCommand { get; }

		public ICommand AddChildLayerCommand { get; }

		public ICommand ShowConfigurationFixesCommand { get; }

		public Point Location
		{
			get => _location;
			set => SetLocation(value, true);
		}

		public Brush BorderBrush => IsActive ? _theme.ActiveConnection : _theme.Border;

		public Thickness BorderThickness => new(IsActive ? 3 : 1);

		public void Attach(NodifyGraphBoundaryViewModel? boundary)
		{
			_containingBoundary = boundary;
		}

		public void MoveBy(Vector delta, bool constrain)
		{
			SetLocation(Location + delta, constrain);
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

		private void ShowConfigurationFixes()
		{
			_selectionHandler?.Invoke(ArchitectureGraphSelection.ForLayer(EditHandle));
		}

		private void SetLocation(Point value, bool constrain)
		{
			var nextLocation = constrain ? CoerceLocation(value) : value;
			if (_location == nextLocation)
			{
				return;
			}

			_location = nextLocation;
			_layoutState.SetLocation(Path, _location);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
			_containingBoundary?.RefreshMinimumSize();
		}

		private Point CoerceLocation(Point value)
		{
			if (_containingBoundary is null)
			{
				return value;
			}

			var minX = _containingBoundary.Location.X + BoundaryPaddingX;
			var minY = _containingBoundary.Location.Y + BoundaryPaddingTop;
			var maxX = _containingBoundary.Location.X + Math.Max(BoundaryPaddingX, _containingBoundary.Width - NodeWidth - BoundaryPaddingX);
			var maxY = _containingBoundary.Location.Y + Math.Max(BoundaryPaddingTop, _containingBoundary.Height - NodeHeight - BoundaryPaddingBottom);
			var result = new Point(Clamp(value.X, minX, maxX), Clamp(value.Y, minY, maxY));

			return result;
		}
	}
}
