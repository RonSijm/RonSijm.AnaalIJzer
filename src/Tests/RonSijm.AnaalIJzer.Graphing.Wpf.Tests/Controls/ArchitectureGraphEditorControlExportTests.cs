using System.IO;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media.Imaging;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf.Controls;
using Xunit;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphCanvasTests
{
	[Fact]
	public void GraphEditorControl_ExportsRenderedGraphsAsPng()
	{
		RunOnStaThread(() =>
		{
			var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphExportTests", Guid.NewGuid().ToString("N"));
			var path = Path.Combine(directory, "graph.png");
			try
			{
				var control = new ArchitectureGraphEditorControl(CreateSimpleSnapshot(), ArchitectureGraphFocusMode.ShowAll);
				control.Measure(new Size(1280, 860));
				control.Arrange(new Rect(0, 0, 1280, 860));
				control.UpdateLayout();
				DrainDispatcher();

				control.ExportGraphsAsPng(path);

				File.Exists(path).Should().BeTrue();
				var bytes = File.ReadAllBytes(path);
				bytes.Length.Should().BeGreaterThan(1024);
				bytes.Take(8).Should().Equal([137, 80, 78, 71, 13, 10, 26, 10]);
			}
			finally
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		});
	}

	[Fact]
	public void GraphEditorControl_ExportSizingExpandsTallGraphs()
	{
		RunOnStaThread(() =>
		{
			var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphExportTests", Guid.NewGuid().ToString("N"));
			var path = Path.Combine(directory, "tall-graph.png");
			try
			{
				var control = new ArchitectureGraphEditorControl(CreateTallSnapshot(), ArchitectureGraphFocusMode.ShowAll, useExportSizing: true);
				control.Measure(new Size(1280, 860));
				control.Arrange(new Rect(0, 0, 1280, 860));
				control.UpdateLayout();
				DrainDispatcher();

				control.ExportGraphsAsPng(path);

				using var stream = File.OpenRead(path);
				var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
				decoder.Frames[0].PixelHeight.Should().BeGreaterThan(1200);
			}
			finally
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		});
	}

	private static ArchitectureGraphSnapshot CreateTallSnapshot()
	{
		var layers = ImmutableArray.CreateBuilder<ArchitectureGraphLayer>();
		layers.Add(new ArchitectureGraphLayer("Caller", "Caller", null, 0, 1, true));
		for (var index = 0; index < 18; index++)
		{
			var name = "Dependency" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
			layers.Add(new ArchitectureGraphLayer(name, name, null, 0, index % 16 + 1, false));
		}

		var rules = ImmutableArray.CreateBuilder<ArchitectureGraphRule>();
		foreach (var layer in layers.Skip(1))
		{
			rules.Add(new ArchitectureGraphRule("Caller", layer.Path, string.Empty, "AllowedDependency", "all sites", false, false, true));
		}

		var result = new ArchitectureGraphSnapshot(true, false, layers.ToImmutable(), rules.ToImmutable(), ImmutableArray.Create("Caller"), ImmutableArray<string>.Empty);

		return result;
	}
}
