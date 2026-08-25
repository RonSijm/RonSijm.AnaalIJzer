using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

public static partial class ArchitectureGraphThemeStyles
{
	public static Style CreateTextBlockStyle(object foreground, object? fontFamily = null, object? fontSize = null)
	{
		var style = new Style(typeof(TextBlock));
		style.Setters.Add(new Setter(TextElement.ForegroundProperty, foreground));
		AddFontSetters(style, fontFamily, fontSize, textElement: true);

		return style;
	}

	public static Style CreateTextBoxStyle(object background, object foreground, object border, object selection, object? fontFamily = null, object? fontSize = null)
	{
		var style = CreateEditControlStyle(typeof(TextBox), background, foreground, border, fontFamily, fontSize);
		style.Setters.Add(new Setter(TextBoxBase.CaretBrushProperty, foreground));
		style.Setters.Add(new Setter(TextBoxBase.SelectionBrushProperty, selection));

		return style;
	}

	public static Style CreateEditControlStyle(Type controlType, object background, object foreground, object border, object? fontFamily = null, object? fontSize = null)
	{
		var style = CreateForegroundControlStyle(controlType, foreground, fontFamily, fontSize);
		style.Setters.Add(new Setter(Control.BackgroundProperty, background));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4, 2, 4, 2)));

		return style;
	}

	public static Style CreateForegroundControlStyle(Type controlType, object foreground, object? fontFamily = null, object? fontSize = null)
	{
		var style = new Style(controlType);
		style.Setters.Add(new Setter(Control.ForegroundProperty, foreground));
		AddFontSetters(style, fontFamily, fontSize, textElement: false);

		return style;
	}

	public static Style CreateButtonStyle(object background, object foreground, object border, object hoverBackground, object pressedBackground, object disabledForeground, object? fontFamily = null, object? fontSize = null)
	{
		var style = CreateForegroundControlStyle(typeof(Button), foreground, fontFamily, fontSize);
		style.Setters.Add(new Setter(Control.BackgroundProperty, background));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 3, 10, 3)));
		style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));
		style.Triggers.Add(CreateSetterTrigger(UIElement.IsMouseOverProperty, true, Control.BackgroundProperty, hoverBackground));
		style.Triggers.Add(CreateSetterTrigger(ButtonBase.IsPressedProperty, true, Control.BackgroundProperty, pressedBackground));
		style.Triggers.Add(CreateSetterTrigger(UIElement.IsEnabledProperty, false, Control.ForegroundProperty, disabledForeground));

		return style;
	}

	public static Style CreateComboBoxItemStyle(object background, object foreground, object border, object hoverBackground, object selectedBackground, object disabledForeground, object? fontFamily = null, object? fontSize = null)
	{
		var style = CreateForegroundControlStyle(typeof(ComboBoxItem), foreground, fontFamily, fontSize);
		style.Setters.Add(new Setter(Control.BackgroundProperty, background));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 3, 6, 3)));
		style.Setters.Add(new Setter(Control.TemplateProperty, CreateComboBoxItemTemplate()));
		style.Triggers.Add(CreateSetterTrigger(ComboBoxItem.IsHighlightedProperty, true, Control.BackgroundProperty, hoverBackground));
		style.Triggers.Add(CreateSetterTrigger(Selector.IsSelectedProperty, true, Control.BackgroundProperty, selectedBackground));
		style.Triggers.Add(CreateSetterTrigger(UIElement.IsEnabledProperty, false, Control.ForegroundProperty, disabledForeground));

		return style;
	}

	public static Style CreateContextMenuStyle(object background, object foreground, object border, object hoverBackground, object disabledForeground, object? fontFamily = null, object? fontSize = null)
	{
		var style = new Style(typeof(ContextMenu));
		style.Setters.Add(new Setter(Control.BackgroundProperty, background));
		style.Setters.Add(new Setter(Control.ForegroundProperty, foreground));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
		style.Setters.Add(new Setter(ItemsControl.ItemContainerStyleProperty, CreateMenuItemStyle(background, foreground, border, hoverBackground, disabledForeground, fontFamily, fontSize)));
		AddFontSetters(style, fontFamily, fontSize, textElement: false);

		return style;
	}

	public static Style CreateMenuItemStyle(object background, object foreground, object border, object hoverBackground, object disabledForeground, object? fontFamily = null, object? fontSize = null)
	{
		var style = CreateForegroundControlStyle(typeof(MenuItem), foreground, fontFamily, fontSize);
		style.Setters.Add(new Setter(Control.BackgroundProperty, background));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));
		style.Triggers.Add(CreateSetterTrigger(MenuItem.IsHighlightedProperty, true, Control.BackgroundProperty, hoverBackground));
		style.Triggers.Add(CreateSetterTrigger(UIElement.IsEnabledProperty, false, Control.ForegroundProperty, disabledForeground));

		return style;
	}

	public static Style CreateSeparatorStyle(object border)
	{
		var style = new Style(typeof(Separator));
		style.Setters.Add(new Setter(Control.BackgroundProperty, border));
		style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 4)));

		return style;
	}

	public static void ApplyPopupResources(FrameworkElement target, object background, object foreground, object border, object hoverBackground, object pressedBackground, object disabledForeground, object selection, object? fontFamily = null, object? fontSize = null)
	{
		target.Resources[typeof(TextBlock)] = CreateTextBlockStyle(foreground, fontFamily, fontSize);
		target.Resources[typeof(TextBox)] = CreateTextBoxStyle(background, foreground, border, selection, fontFamily, fontSize);
		target.Resources[typeof(CheckBox)] = CreateForegroundControlStyle(typeof(CheckBox), foreground, fontFamily, fontSize);
		target.Resources[typeof(ComboBox)] = CreateComboBoxStyle(background, foreground, border, hoverBackground, pressedBackground, disabledForeground, fontFamily, fontSize);
		target.Resources[typeof(ComboBoxItem)] = CreateComboBoxItemStyle(background, foreground, border, hoverBackground, pressedBackground, disabledForeground, fontFamily, fontSize);
		target.Resources[typeof(Expander)] = CreateForegroundControlStyle(typeof(Expander), foreground, fontFamily, fontSize);
		target.Resources[typeof(Button)] = CreateButtonStyle(background, foreground, border, hoverBackground, pressedBackground, disabledForeground, fontFamily, fontSize);
		target.Resources[typeof(ContextMenu)] = CreateContextMenuStyle(background, foreground, border, hoverBackground, disabledForeground, fontFamily, fontSize);
		target.Resources[typeof(MenuItem)] = CreateMenuItemStyle(background, foreground, border, hoverBackground, disabledForeground, fontFamily, fontSize);
		target.Resources[typeof(Separator)] = CreateSeparatorStyle(border);
	}
}
