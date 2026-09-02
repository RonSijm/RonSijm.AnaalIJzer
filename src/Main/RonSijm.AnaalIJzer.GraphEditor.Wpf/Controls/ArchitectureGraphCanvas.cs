using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Nodify;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas : UserControl
{
	private const double NodeWidth = 170;
	private const double NodeHeight = 72;
	private const double BoundaryPaddingX = 28;
	private const double BoundaryPaddingTop = 36;
	private const double BoundaryPaddingBottom = 24;
	private const uint GridCellSize = 16;

	private readonly ArchitectureGraphGroupViewModel _group;
	private readonly Action<ArchitectureConfigurationEditResult, bool>? _editResultHandler;
	private readonly Action<ArchitectureGraphSelection>? _selectionHandler;
	private readonly Func<string, bool>? _confirmationHandler;
	private readonly Func<ArchitectureLayerCreationRequest?> _layerCreationHandler;
	private readonly ArchitectureGraphCanvasTheme _theme;
	private readonly ILogger? _logger;
	private readonly ArchitectureGraphLayoutState _layoutState;
	private readonly IArchitectureGraphEditService _editService;
	private readonly bool _useExportMode;

	public ArchitectureGraphCanvas(
		ArchitectureGraphGroupViewModel group,
		Action<ArchitectureConfigurationEditResult, bool>? editResultHandler = null,
		Action<ArchitectureGraphSelection>? selectionHandler = null,
		Func<string, bool>? confirmationHandler = null,
		ArchitectureGraphCanvasTheme? theme = null,
		ILogger? logger = null,
		Func<ArchitectureLayerCreationRequest?>? layerCreationHandler = null,
		ArchitectureGraphLayoutState? layoutState = null,
		IArchitectureGraphEditService? editService = null,
		bool useExportMode = false)
	{
		this._group = group;
		this._editResultHandler = editResultHandler;
		this._selectionHandler = selectionHandler;
		this._confirmationHandler = confirmationHandler;
		this._theme = theme ?? ArchitectureGraphCanvasTheme.Default;
		this._logger = logger;
		this._layerCreationHandler = layerCreationHandler ?? PromptForLayerCreation;
		this._layoutState = layoutState ?? ArchitectureGraphLayoutState.Load(group.ConfigurationSource);
		this._editService = editService ?? new ArchitectureGraphEditService();
		this._useExportMode = useExportMode;
		PreviewMouseLeftButtonUp += (_, _) => this._layoutState.Save();
		Unloaded += (_, _) => this._layoutState.Save();
		BuildSurface();
	}

	private void BuildSurface()
	{
		try
		{
			var graph = NodifyGraphViewModel.Create(_group, _editService, _editResultHandler, _selectionHandler, _confirmationHandler, _layerCreationHandler, _layoutState, _theme);
			_logger?.LogDebug(
				"Building Nodify canvas for '{Title}'. Nodes: {NodeCount}. Connections: {ConnectionCount}.",
				_group.Title,
				graph.Nodes.Length,
				graph.Connections.Length);
			var editor = new NodifyEditor
			{
				ItemsSource = graph.Items,
				Connections = graph.Connections,
				ItemTemplateSelector = new NodifyGraphItemTemplateSelector(CreateBoundaryTemplate(), CreateNodeTemplate()),
				ItemContainerStyle = CreateItemContainerStyle(),
				ConnectionTemplate = CreateConnectionTemplate(),
				Background = CreateGridBrush(_theme),
				GridCellSize = GridCellSize,
				MinViewportZoom = 0.35,
				MaxViewportZoom = 2.5,
				ViewportZoom = 0.9,
				ViewportLocation = new Point(-24, -24),
				DisplayConnectionsOnTop = false,
				HasCustomContextMenu = true,
				PendingConnection = new object(),
				PendingConnectionTemplate = CreatePendingConnectionTemplate(),
				ConnectionCompletedCommand = new DelegateCommand(CompleteConnection),
				ContextMenu = CreateCanvasContextMenu(),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			var root = new Grid { Background = _theme.SurfaceBackground };
			root.Children.Add(editor);
			if (!_useExportMode)
			{
				root.Children.Add(CreateMinimap(editor));
			}

			Content = root;
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to build Nodify canvas for '{Title}'.", _group.Title);
			throw;
		}
	}

	private static Brush CreateGridBrush(ArchitectureGraphCanvasTheme theme)
	{
		var drawing = new DrawingGroup();
		drawing.Children.Add(new GeometryDrawing(theme.SurfaceBackground, null, new RectangleGeometry(new Rect(0, 0, GridCellSize, GridCellSize))));
		drawing.Children.Add(new GeometryDrawing(theme.GridLine, null, Geometry.Parse("M0,0 L0,1 0.04,1 0.04,0.04 1,0.04 1,0 Z")));
		var brush = new DrawingBrush(drawing)
		{
			TileMode = TileMode.Tile,
			ViewportUnits = BrushMappingMode.Absolute,
			Viewport = new Rect(0, 0, GridCellSize, GridCellSize),
			Stretch = Stretch.None,
			Opacity = 0.75
		};
		brush.Freeze();

		return brush;
	}

	private static string GetParentPath(string path)
	{
		var slashIndex = path.LastIndexOf('/');
		var result = slashIndex <= 0 ? string.Empty : path.Substring(0, slashIndex);

		return result;
	}

	private static double Clamp(double value, double minimum, double maximum)
	{
		var result = Math.Min(Math.Max(value, minimum), Math.Max(minimum, maximum));

		return result;
	}
}
