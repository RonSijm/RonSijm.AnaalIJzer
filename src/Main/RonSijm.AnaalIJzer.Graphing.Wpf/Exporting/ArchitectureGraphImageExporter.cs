using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Exporting;

public static class ArchitectureGraphImageExporter
{
	private const double Dpi = 96;

	public static void SavePng(FrameworkElement element, string path, Brush? background = null)
	{
		var bitmap = RenderToBitmap(element, background);
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var encoder = new PngBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using var stream = File.Create(path);
		encoder.Save(stream);
	}

	public static RenderTargetBitmap RenderToBitmap(FrameworkElement element, Brush? background = null)
	{
		var size = MeasureElement(element);
		var pixelWidth = Math.Max(1, (int)Math.Ceiling(size.Width));
		var pixelHeight = Math.Max(1, (int)Math.Ceiling(size.Height));
		var visual = new DrawingVisual();
		using (var context = visual.RenderOpen())
		{
			context.DrawRectangle(background ?? Brushes.Transparent, null, new Rect(0, 0, size.Width, size.Height));
			context.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, size.Width, size.Height));
		}

		var result = new RenderTargetBitmap(pixelWidth, pixelHeight, Dpi, Dpi, PixelFormats.Pbgra32);
		result.Render(visual);

		return result;
	}

	private static Size MeasureElement(FrameworkElement element)
	{
		element.UpdateLayout();
		var width = element.ActualWidth > 0 ? element.ActualWidth : element.DesiredSize.Width;
		var height = element.ActualHeight > 0 ? element.ActualHeight : element.DesiredSize.Height;
		if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height))
		{
			element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			element.Arrange(new Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));
			element.UpdateLayout();
			width = element.ActualWidth > 0 ? element.ActualWidth : element.DesiredSize.Width;
			height = element.ActualHeight > 0 ? element.ActualHeight : element.DesiredSize.Height;
		}

		if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height))
		{
			throw new InvalidOperationException("The graph surface has no rendered size to export.");
		}

		var result = new Size(width, height);

		return result;
	}
}
