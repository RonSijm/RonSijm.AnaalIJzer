using System.ComponentModel;
using System.Windows;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifyGraphConnectorViewModel(string layerPath, string title, bool isOutput)
		: INotifyPropertyChanged
	{
		private Point anchor;

		public event PropertyChangedEventHandler? PropertyChanged;

		public string LayerPath { get; } = layerPath;

		public string Title { get; } = title;

		public bool IsOutput { get; } = isOutput;

		public bool IsInput
		{
			get
			{
				var result = !IsOutput;

				return result;
			}
		}

		public Point Anchor
		{
			get { return anchor; }
			set
			{
				if (anchor == value)
				{
					return;
				}

				anchor = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
			}
		}

		public string ToolTip
		{
			get
			{
				var result = LayerPath + " " + Title + " connector";

				return result;
			}
		}
	}
}
