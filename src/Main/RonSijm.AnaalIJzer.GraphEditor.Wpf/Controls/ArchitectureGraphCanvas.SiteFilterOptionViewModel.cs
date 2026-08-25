using System.ComponentModel;
using System.Windows.Input;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifySiteFilterOptionViewModel(string site, bool isChecked, ICommand command)
		: INotifyPropertyChanged
	{
		private bool isChecked = isChecked;

		public event PropertyChangedEventHandler? PropertyChanged;

		public string Site { get; } = site;

		public bool IsChecked
		{
			get { return isChecked; }
			set
			{
				if (isChecked == value)
				{
					return;
				}

				isChecked = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
			}
		}

		public ICommand Command { get; } = command;
	}
}
