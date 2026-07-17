using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.Graphing.Building;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Controls;
using RonSijm.AnaalIJzer.Graphing.Wpf.Exporting;
using RonSijm.AnaalIJzer.Tooling;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed class GraphImageExportCommand
{
	private const int DefaultWidth = 1600;
	private const int DefaultHeight = 1000;
	private const double ExportMinimumWidth = 680;
	private const double ExportMinimumHeight = 320;
	private const double ExportNodeWidth = 210;
	private const double ExportNodeHeight = 112;
	private const double ExportPadding = 96;
	private const double ExportGroupChromeHeight = 96;
	private readonly GraphImageExportMode mode;
	private readonly string inputPath;
	private readonly string outputPath;
	private readonly string configuration;
	private readonly int width;
	private readonly int height;
	private readonly bool failOnError;

	private GraphImageExportCommand(GraphImageExportMode mode, string inputPath, string outputPath, string configuration, int width, int height, bool failOnError)
	{
		this.mode = mode;
		this.inputPath = inputPath;
		this.outputPath = outputPath;
		this.configuration = configuration;
		this.width = width;
		this.height = height;
		this.failOnError = failOnError;
	}

	public static bool TryCreate(string[] args, out GraphImageExportCommand? command, out string? error)
	{
		command = null;
		error = null;
		var exportIndex = IndexOf(args, "--export");
		var examplesIndex = IndexOf(args, "--export-examples");
		if (exportIndex < 0 && examplesIndex < 0)
		{
			return false;
		}

		if (exportIndex >= 0 && examplesIndex >= 0)
		{
			error = "Use either --export or --export-examples, not both.";

			return true;
		}

		var configuration = GetOptionValue(args, "--configuration") ?? "Release";
		var width = GetIntOptionValue(args, "--width", DefaultWidth);
		var height = GetIntOptionValue(args, "--height", DefaultHeight);
		var failOnError = HasOption(args, "--fail-on-error");
		if (width <= 0 || height <= 0)
		{
			error = "--width and --height must be positive numbers.";

			return true;
		}

		if (exportIndex >= 0)
		{
			if (!TryGetPositionalPair(args, exportIndex, out var input, out var output, out error))
			{
				return true;
			}

			command = new GraphImageExportCommand(GraphImageExportMode.Single, input, output, configuration, width, height, failOnError);

			return true;
		}

		if (!TryGetPositionalPair(args, examplesIndex, out var examplesRoot, out var outputDirectory, out error))
		{
			return true;
		}

		command = new GraphImageExportCommand(GraphImageExportMode.Examples, examplesRoot, outputDirectory, configuration, width, height, failOnError);

		return true;
	}

	public int Execute(ILogger logger, CancellationToken cancellationToken = default)
	{
		var result = mode == GraphImageExportMode.Single
			? ExportSingle(inputPath, outputPath, logger, cancellationToken)
			: ExportExamples(inputPath, outputPath, logger, cancellationToken);

		return result;
	}

	private int ExportSingle(string input, string output, ILogger logger, CancellationToken cancellationToken)
	{
		var loaded = TryExport(input, output, logger, false, cancellationToken);
		var result = loaded ? 0 : 1;

		return result;
	}

	private int ExportExamples(string examplesRoot, string outputDirectory, ILogger logger, CancellationToken cancellationToken)
	{
		var fullExamplesRoot = Path.GetFullPath(examplesRoot);
		if (!Directory.Exists(fullExamplesRoot))
		{
			throw new DirectoryNotFoundException("Examples directory was not found: " + fullExamplesRoot);
		}

		Directory.CreateDirectory(outputDirectory);
		var projectPaths = Directory
			.EnumerateFiles(fullExamplesRoot, "*.csproj", SearchOption.AllDirectories)
			.Order(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		var successCount = 0;
		var placeholderCount = 0;
		foreach (var projectPath in projectPaths)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var outputPath = CreateExampleOutputPath(fullExamplesRoot, projectPath, outputDirectory);
			var exported = TryExport(projectPath, outputPath, logger, true, cancellationToken);
			if (exported)
			{
				successCount++;
			}
			else
			{
				placeholderCount++;
			}
		}

		Console.WriteLine("Exported " + successCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " example graph image(s) to " + outputDirectory + ".");
		if (placeholderCount > 0)
		{
			Console.WriteLine("Created " + placeholderCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " placeholder image(s) for examples that could not render a graph.");
		}

		var result = placeholderCount > 0 && failOnError ? 1 : 0;

		return result;
	}

	private bool TryExport(string input, string output, ILogger logger, bool writePlaceholderOnFailure, CancellationToken cancellationToken)
	{
		try
		{
			var loader = new ArchitectureGraphWorkspaceSnapshotLoader(configuration);
			var snapshot = loader.LoadAsync(input, cancellationToken).GetAwaiter().GetResult();
			var control = CreateControl(snapshot);
			if (!control.HasExportableGraphs)
			{
				throw new InvalidOperationException(CreateNoGraphMessage(snapshot));
			}

			EnsureOutputDirectory(output);
			control.ExportGraphsAsPng(output);
			Console.WriteLine("Wrote " + output);
			logger.LogInformation("Exported architecture graph image. Input: {Input}. Output: {Output}.", input, output);

			return true;
		}
		catch (Exception exception) when (writePlaceholderOnFailure)
		{
			logger.LogWarning(exception, "Failed to export example graph image for {Input}. Writing placeholder to {Output}.", input, output);
			EnsureOutputDirectory(output);
			ExportPlaceholder(output, Path.GetFileNameWithoutExtension(input), exception.Message);
			Console.WriteLine("Wrote placeholder " + output + " (" + exception.Message + ")");

			return false;
		}
	}

	private ArchitectureGraphEditorControl CreateControl(ArchitectureGraphSnapshot snapshot)
	{
		var control = new ArchitectureGraphEditorControl(snapshot, ArchitectureGraphFocusMode.ShowAll, logger: null, useExportSizing: true);
		var size = CalculateExportSize(snapshot);
		control.Measure(size);
		var arrangedSize = new Size(size.Width, Math.Max(size.Height, control.DesiredSize.Height));
		control.Arrange(new Rect(arrangedSize));
		control.UpdateLayout();
		DrainDispatcher();

		return control;
	}

	private Size CalculateExportSize(ArchitectureGraphSnapshot snapshot)
	{
		var groups = ArchitectureGraphViewModelBuilder.Build(
			snapshot,
			ArchitectureGraphFocusMode.ShowAll,
			snapshot.Evidence.HasEvidence);
		if (groups.Length == 0)
		{
			return new Size(width, height);
		}

		var contentWidth = groups.Max(CalculateGroupExportWidth);
		var contentHeight = groups.Sum(CalculateGroupExportHeight);
		var exportWidth = Math.Min(width, Math.Max(ExportMinimumWidth, contentWidth));
		var exportHeight = Math.Max(ExportMinimumHeight, contentHeight);
		var result = new Size(Math.Ceiling(exportWidth), Math.Ceiling(exportHeight));

		return result;
	}

	private static double CalculateGroupExportWidth(ArchitectureGraphGroupViewModel group)
	{
		var maxNodeX = group.Nodes.Length == 0
			? 0
			: group.Nodes.Max(node => node.X + ExportNodeWidth);
		var maxBoundaryX = group.Boundaries.Length == 0
			? 0
			: group.Boundaries.Max(boundary => boundary.X + boundary.Width);
		var result = Math.Max(maxNodeX, maxBoundaryX) + ExportPadding;

		return result;
	}

	private static double CalculateGroupExportHeight(ArchitectureGraphGroupViewModel group)
	{
		if (group.Nodes.Length == 0)
		{
			return ExportMinimumHeight;
		}

		var maxNodeY = group.Nodes.Max(node => node.Y + ExportNodeHeight);
		var maxBoundaryY = group.Boundaries.Length == 0
			? 0
			: group.Boundaries.Max(boundary => boundary.Y + boundary.Height);
		var result = Math.Max(ExportMinimumHeight, Math.Max(maxNodeY, maxBoundaryY) + ExportPadding + ExportGroupChromeHeight);

		return result;
	}

	private void ExportPlaceholder(string output, string title, string message)
	{
		var stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = "No graph image was generated for " + title,
			FontSize = 18,
			FontWeight = FontWeights.SemiBold,
			Foreground = SystemColors.ControlTextBrush,
			TextWrapping = TextWrapping.Wrap
		});
		stack.Children.Add(new TextBlock
		{
			Text = message,
			Margin = new Thickness(0, 12, 0, 0),
			Foreground = SystemColors.ControlTextBrush,
			TextWrapping = TextWrapping.Wrap
		});
		var border = new Border
		{
			Width = width,
			Height = Math.Min(height, 420),
			Padding = new Thickness(24),
			Background = SystemColors.WindowBrush,
			BorderBrush = SystemColors.ActiveBorderBrush,
			BorderThickness = new Thickness(1),
			Child = stack
		};
		var size = new Size(width, Math.Min(height, 420));
		border.Measure(size);
		border.Arrange(new Rect(size));
		border.UpdateLayout();
		ArchitectureGraphImageExporter.SavePng(border, output, SystemColors.WindowBrush);
	}

	private static string CreateExampleOutputPath(string examplesRoot, string projectPath, string outputDirectory)
	{
		var fileName = Path.GetFileNameWithoutExtension(projectPath) + "-Graph.png";
		var result = Path.Combine(outputDirectory, fileName);

		return result;
	}

	private static string CreateNoGraphMessage(ArchitectureGraphSnapshot snapshot)
	{
		var result = snapshot.ConfigurationIssueMessages.Length > 0
			? string.Join(Environment.NewLine, snapshot.ConfigurationIssueMessages)
			: "No renderable dependency graph was found.";

		return result;
	}

	private static void EnsureOutputDirectory(string output)
	{
		var directory = Path.GetDirectoryName(Path.GetFullPath(output));
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}
	}

	private static void DrainDispatcher()
	{
		var frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
		Dispatcher.PushFrame(frame);
	}

	private static bool TryGetPositionalPair(string[] args, int commandIndex, out string first, out string second, out string? error)
	{
		first = string.Empty;
		second = string.Empty;
		error = null;
		if (commandIndex + 2 >= args.Length)
		{
			error = args[commandIndex] + " expects an input path and an output path.";

			return false;
		}

		first = args[commandIndex + 1];
		second = args[commandIndex + 2];
		if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
		{
			error = args[commandIndex] + " expects non-empty input and output paths.";

			return false;
		}

		return true;
	}

	private static string? GetOptionValue(string[] args, string option)
	{
		var index = IndexOf(args, option);
		if (index < 0 || index + 1 >= args.Length)
		{
			return null;
		}

		var result = args[index + 1];

		return result;
	}

	private static int GetIntOptionValue(string[] args, string option, int fallback)
	{
		var value = GetOptionValue(args, option);
		var result = int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: fallback;

		return result;
	}

	private static bool HasOption(string[] args, string option)
	{
		var result = IndexOf(args, option) >= 0;

		return result;
	}

	private static int IndexOf(string[] args, string option)
	{
		for (var index = 0; index < args.Length; index++)
		{
			if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
			{
				return index;
			}
		}

		return -1;
	}
}

internal enum GraphImageExportMode
{
	Single,
	Examples
}
