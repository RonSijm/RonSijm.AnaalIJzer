using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

public static class ArchitectureGraphThemeStyles
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

	private static Style CreateComboBoxStyle(object background, object foreground, object border, object hoverBackground, object pressedBackground, object disabledForeground, object? fontFamily, object? fontSize)
	{
		var style = CreateEditControlStyle(typeof(ComboBox), background, foreground, border, fontFamily, fontSize);
		style.Setters.Add(new Setter(Control.TemplateProperty, CreateComboBoxTemplate()));
		style.Setters.Add(new Setter(ItemsControl.ItemContainerStyleProperty, CreateComboBoxItemStyle(background, foreground, border, hoverBackground, pressedBackground, disabledForeground, fontFamily, fontSize)));
		style.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ComboBoxPreviewMouseLeftButtonDown)));
		style.Triggers.Add(CreateSetterTrigger(UIElement.IsMouseOverProperty, true, Control.BackgroundProperty, hoverBackground));
		style.Triggers.Add(CreateSetterTrigger(ComboBox.IsDropDownOpenProperty, true, Control.BackgroundProperty, pressedBackground));
		style.Triggers.Add(CreateSetterTrigger(UIElement.IsEnabledProperty, false, Control.ForegroundProperty, disabledForeground));

		return style;
	}

	private static ControlTemplate CreateButtonTemplate()
	{
		var chrome = new FrameworkElementFactory(typeof(Border));
		chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
		chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

		var content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		content.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
		chrome.AppendChild(content);

		var result = new ControlTemplate(typeof(Button)) { VisualTree = chrome };

		return result;
	}

	private static ControlTemplate CreateComboBoxTemplate()
	{
		var chrome = new FrameworkElementFactory(typeof(Border));
		chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
		chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

		var grid = new FrameworkElementFactory(typeof(Grid));
		chrome.AppendChild(grid);

		var content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemProperty));
		content.SetValue(ContentPresenter.ContentStringFormatProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemStringFormatProperty));
		content.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemTemplateProperty));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(ContentPresenter.MarginProperty, new Thickness(6, 2, 24, 2));
		content.SetValue(UIElement.IsHitTestVisibleProperty, false);
		grid.AppendChild(content);

		var arrow = new FrameworkElementFactory(typeof(Path));
		arrow.SetValue(Path.DataProperty, Geometry.Parse("M 0 0 L 4 4 L 8 0 Z"));
		arrow.SetValue(Path.FillProperty, new TemplateBindingExtension(Control.ForegroundProperty));
		arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
		arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
		grid.AppendChild(arrow);

		var toggle = new FrameworkElementFactory(typeof(ToggleButton));
		toggle.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
		toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
		toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0));
		toggle.SetValue(Control.TemplateProperty, CreateTransparentToggleButtonTemplate());
		toggle.SetValue(UIElement.FocusableProperty, false);
		toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen")
		{
			Mode = BindingMode.TwoWay,
			RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		toggle.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ComboBoxToggleClicked));
		grid.AppendChild(toggle);

		var popup = new FrameworkElementFactory(typeof(Popup));
		popup.Name = "PART_Popup";
		popup.SetValue(Popup.AllowsTransparencyProperty, true);
		popup.SetValue(Popup.FocusableProperty, false);
		popup.SetValue(Popup.IsOpenProperty, new TemplateBindingExtension(ComboBox.IsDropDownOpenProperty));
		popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
		popup.SetBinding(Popup.PlacementTargetProperty, new Binding
		{
			RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
		});
		popup.SetValue(Popup.StaysOpenProperty, false);

		var popupBorder = new FrameworkElementFactory(typeof(Border));
		popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
		popupBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		popupBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		popupBorder.SetValue(FrameworkElement.MinWidthProperty, new TemplateBindingExtension(FrameworkElement.ActualWidthProperty));

		var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
		scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);
		scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
		scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

		var items = new FrameworkElementFactory(typeof(ItemsPresenter));
		scrollViewer.AppendChild(items);
		popupBorder.AppendChild(scrollViewer);
		popup.AppendChild(popupBorder);
		grid.AppendChild(popup);

		var result = new ControlTemplate(typeof(ComboBox)) { VisualTree = chrome };

		return result;
	}

	private static void ComboBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is not ComboBox comboBox || !comboBox.IsEnabled)
		{
			return;
		}

		comboBox.Focus();
		comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
		e.Handled = true;
	}

	private static void ComboBoxToggleClicked(object sender, RoutedEventArgs e)
	{
		if (sender is not ToggleButton { TemplatedParent: ComboBox comboBox } toggle)
		{
			return;
		}

		var isOpen = toggle.IsChecked == true;
		comboBox.IsDropDownOpen = isOpen;
	}

	private static ControlTemplate CreateTransparentToggleButtonTemplate()
	{
		var border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

		var result = new ControlTemplate(typeof(ToggleButton)) { VisualTree = border };

		return result;
	}

	private static ControlTemplate CreateComboBoxItemTemplate()
	{
		var chrome = new FrameworkElementFactory(typeof(Border));
		chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

		var content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
		chrome.AppendChild(content);

		var result = new ControlTemplate(typeof(ComboBoxItem)) { VisualTree = chrome };

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
