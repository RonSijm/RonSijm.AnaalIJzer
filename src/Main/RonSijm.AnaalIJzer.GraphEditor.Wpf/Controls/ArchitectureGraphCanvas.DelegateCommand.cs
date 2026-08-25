using System.Windows.Input;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed class DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
		: ICommand
	{
		public event EventHandler? CanExecuteChanged;

		public bool CanExecute(object? parameter)
		{
			var result = canExecute?.Invoke(parameter) ?? true;

			return result;
		}

		public void Execute(object? parameter)
		{
			execute(parameter);
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
