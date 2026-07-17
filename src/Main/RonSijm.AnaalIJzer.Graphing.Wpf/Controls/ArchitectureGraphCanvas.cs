using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Nodify;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphCanvas : UserControl
{
	private const double NodeWidth = 170;
	private const double NodeHeight = 72;
	private const double BoundaryPaddingX = 28;
	private const double BoundaryPaddingTop = 36;
	private const double BoundaryPaddingBottom = 24;
	private const uint GridCellSize = 16;

	private readonly ArchitectureGraphGroupViewModel group;
	private readonly Action<ArchitectureConfigurationEditResult, bool>? editResultHandler;
	private readonly Action<ArchitectureGraphSelection>? selectionHandler;
	private readonly Func<string, bool>? confirmationHandler;
	private readonly Func<ArchitectureLayerCreationRequest?> layerCreationHandler;
	private readonly ArchitectureGraphCanvasTheme theme;
	private readonly ILogger? logger;
	private readonly ArchitectureGraphLayoutState layoutState;
	private readonly bool useExportMode;

	public ArchitectureGraphCanvas(
		ArchitectureGraphGroupViewModel group,
		Action<ArchitectureConfigurationEditResult, bool>? editResultHandler = null,
		Action<ArchitectureGraphSelection>? selectionHandler = null,
		Func<string, bool>? confirmationHandler = null,
		ArchitectureGraphCanvasTheme? theme = null,
		ILogger? logger = null,
		Func<ArchitectureLayerCreationRequest?>? layerCreationHandler = null,
		ArchitectureGraphLayoutState? layoutState = null,
		bool useExportMode = false)
	{
		this.group = group;
		this.editResultHandler = editResultHandler;
		this.selectionHandler = selectionHandler;
		this.confirmationHandler = confirmationHandler;
		this.theme = theme ?? ArchitectureGraphCanvasTheme.Default;
		this.logger = logger;
		this.layerCreationHandler = layerCreationHandler ?? PromptForLayerCreation;
		this.layoutState = layoutState ?? ArchitectureGraphLayoutState.Load(group.ConfigurationSource);
		this.useExportMode = useExportMode;
		PreviewMouseLeftButtonUp += (_, _) => this.layoutState.Save();
		Unloaded += (_, _) => this.layoutState.Save();
		BuildSurface();
	}

	private void BuildSurface()
	{
		try
		{
			var graph = NodifyGraphViewModel.Create(group, editResultHandler, confirmationHandler, layerCreationHandler, layoutState, theme);
			logger?.LogDebug(
				"Building Nodify canvas for '{Title}'. Nodes: {NodeCount}. Connections: {ConnectionCount}.",
				group.Title,
				graph.Nodes.Length,
				graph.Connections.Length);
			var editor = new NodifyEditor
			{
				ItemsSource = graph.Items,
				Connections = graph.Connections,
				ItemTemplateSelector = new NodifyGraphItemTemplateSelector(CreateBoundaryTemplate(), CreateNodeTemplate()),
				ItemContainerStyle = CreateItemContainerStyle(),
				ConnectionTemplate = CreateConnectionTemplate(),
				Background = CreateGridBrush(theme),
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

			var root = new Grid { Background = theme.SurfaceBackground };
			root.Children.Add(editor);
			if (!useExportMode)
			{
				root.Children.Add(CreateMinimap(editor));
			}

			Content = root;
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to build Nodify canvas for '{Title}'.", group.Title);
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
