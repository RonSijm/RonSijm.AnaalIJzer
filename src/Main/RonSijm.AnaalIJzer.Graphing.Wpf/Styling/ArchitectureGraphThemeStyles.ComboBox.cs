using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

public static partial class ArchitectureGraphThemeStyles
{
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
}
