using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Styling;

internal static class ArchitectureVisualStudioTheme
{
	private static readonly object ToolWindowBackgroundKey = VsBrushes.GetBrushKey((int)__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND);
	private static readonly object ToolWindowTextKey = VsBrushes.GetBrushKey((int)__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT);
	private static readonly object ToolWindowBorderKey = VsBrushes.GetBrushKey((int)__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BORDER);
	private static readonly object HintTextKey = VsBrushes.GetBrushKey((int)__VSSYSCOLOREX.VSCOLOR_CONTROL_EDIT_HINTTEXT);

	public static void ApplyToToolWindow(FrameworkElement root)
	{
		root.SetResourceReference(TextElement.ForegroundProperty, ToolWindowTextKey);
		root.SetResourceReference(Control.FontFamilyProperty, VsFonts.EnvironmentFontFamilyKey);
		root.SetResourceReference(Control.FontSizeProperty, VsFonts.EnvironmentFontSizeKey);
		var background = new DynamicResourceExtension(ToolWindowBackgroundKey);
		var foreground = new DynamicResourceExtension(ToolWindowTextKey);
		var border = new DynamicResourceExtension(ToolWindowBorderKey);
		var hint = new DynamicResourceExtension(HintTextKey);
		var fontFamily = new DynamicResourceExtension(VsFonts.EnvironmentFontFamilyKey);
		var fontSize = new DynamicResourceExtension(VsFonts.EnvironmentFontSizeKey);
		ArchitectureGraphThemeStyles.ApplyPopupResources(root, background, foreground, border, border, border, hint, Brushes.DodgerBlue, fontFamily, fontSize);
	}

	public static void ApplyBackground(Panel panel)
	{
		panel.SetResourceReference(Panel.BackgroundProperty, ToolWindowBackgroundKey);
	}

	public static void ApplyBackground(Border border)
	{
		border.SetResourceReference(Border.BackgroundProperty, ToolWindowBackgroundKey);
		border.SetResourceReference(Border.BorderBrushProperty, ToolWindowBorderKey);
	}

	public static void ApplyHintForeground(TextBlock textBlock)
	{
		textBlock.SetResourceReference(TextElement.ForegroundProperty, HintTextKey);
	}

	public static void ApplyNeutralStatusForeground(TextBlock textBlock)
	{
		ApplyHintForeground(textBlock);
	}

	public static void ApplyBorder(Border border, bool highlighted)
	{
		if (highlighted)
		{
			border.BorderBrush = Brushes.DarkOrange;
			return;
		}

		border.SetResourceReference(Border.BorderBrushProperty, ToolWindowBorderKey);
	}

	public static ArchitectureGraphCanvasTheme CreateGraphTheme(FrameworkElement element)
	{
		var background = GetBrush(element, ToolWindowBackgroundKey, SystemColors.WindowBrush);
		var foreground = GetBrush(element, ToolWindowTextKey, SystemColors.ControlTextBrush);
		var border = GetBrush(element, ToolWindowBorderKey, SystemColors.ActiveBorderBrush);
		var connection = GetBrush(element, HintTextKey, new SolidColorBrush(Color.FromRgb(112, 122, 136)));
		var grid = CreateOpacityBrush(foreground, 0.18);
		var result = new ArchitectureGraphCanvasTheme(
			background,
			grid,
			foreground,
			Brushes.White,
			border,
			connection,
			Brushes.DarkOrange,
			Brushes.IndianRed,
			Brushes.White);

		return result;
	}

	public static ArchitectureGraphEditorTheme CreateEditorTheme(FrameworkElement element)
	{
		var background = GetBrush(element, ToolWindowBackgroundKey, SystemColors.WindowBrush);
		var foreground = GetBrush(element, ToolWindowTextKey, SystemColors.ControlTextBrush);
		var border = GetBrush(element, ToolWindowBorderKey, SystemColors.ActiveBorderBrush);
		var hint = GetBrush(element, HintTextKey, SystemColors.GrayTextBrush);
		var result = new ArchitectureGraphEditorTheme(
			background,
			foreground,
			hint,
			border,
			Brushes.DarkOrange,
			Brushes.ForestGreen,
			Brushes.IndianRed,
			CreateGraphTheme(element));

		return result;
	}

	private static Brush GetBrush(FrameworkElement element, object key, Brush fallback)
	{
		var result = element.TryFindResource(key) as Brush ?? fallback;

		return result;
	}

	private static Brush CreateOpacityBrush(Brush source, double opacity)
	{
		if (source is not SolidColorBrush solid)
		{
			return source;
		}

		var result = new SolidColorBrush(Color.FromArgb(
			(byte)Math.Round(byte.MaxValue * opacity),
			solid.Color.R,
			solid.Color.G,
			solid.Color.B));

		return result;
	}
}
