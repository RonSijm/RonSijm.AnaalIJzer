using System.ComponentModel;
using System.Windows;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifyGraphConnectorViewModel(string layerPath, string title, bool isOutput)
		: INotifyPropertyChanged
	{
		private Point _anchor;

		public event PropertyChangedEventHandler? PropertyChanged;

		public string LayerPath { get; } = layerPath;

		public string Title { get; } = title;

		public bool IsOutput { get; } = isOutput;

		public bool IsInput => !IsOutput;

		public Point Anchor
		{
			get { return _anchor; }
			set
			{
				if (_anchor == value)
				{
					return;
				}

				_anchor = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
			}
		}

		public string ToolTip => LayerPath + " " + Title + " connector";
	}
}
