using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

public static partial class ArchitectureGraphThemeStyles
{
	private static ControlTemplate CreateButtonTemplate()
	{
		var chrome = new FrameworkElementFactory(typeof(Border));
		chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
		chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

		var content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		content.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
		chrome.AppendChild(content);

		var result = new ControlTemplate(typeof(Button)) { VisualTree = chrome };

		return result;
	}

	private static Trigger CreateSetterTrigger(DependencyProperty property, object value, DependencyProperty setterProperty, object setterValue)
	{
		var result = new Trigger { Property = property, Value = value };
		result.Setters.Add(new Setter(setterProperty, setterValue));

		return result;
	}

	private static void AddFontSetters(Style style, object? fontFamily, object? fontSize, bool textElement)
	{
		if (fontFamily is not null)
		{
			style.Setters.Add(new Setter(textElement ? TextElement.FontFamilyProperty : Control.FontFamilyProperty, fontFamily));
		}

		if (fontSize is not null)
		{
			style.Setters.Add(new Setter(textElement ? TextElement.FontSizeProperty : Control.FontSizeProperty, fontSize));
		}
	}
}
