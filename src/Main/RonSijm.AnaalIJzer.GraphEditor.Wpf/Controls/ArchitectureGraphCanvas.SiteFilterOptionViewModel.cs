using System.ComponentModel;
using System.Windows.Input;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifySiteFilterOptionViewModel(string site, bool isChecked, ICommand command)
		: INotifyPropertyChanged
	{
		private bool _isChecked = isChecked;

		public event PropertyChangedEventHandler? PropertyChanged;

		public string Site { get; } = site;

		public bool IsChecked
		{
			get => _isChecked;
			set
			{
				if (_isChecked == value)
				{
					return;
				}

				_isChecked = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
			}
		}

		public ICommand Command { get; } = command;
	}
}
