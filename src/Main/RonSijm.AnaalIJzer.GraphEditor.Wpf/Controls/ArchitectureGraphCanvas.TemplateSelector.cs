using System.Windows;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifyGraphItemTemplateSelector(DataTemplate boundaryTemplate, DataTemplate nodeTemplate) : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var result = item is NodifyGraphBoundaryViewModel ? boundaryTemplate : nodeTemplate;

			return result;
		}
	}
}
