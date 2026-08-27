using System.ComponentModel;
using System.Windows;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphBoundaryViewModel
	{
		private void SetLocation(Point value, bool constrain, bool moveContents)
		{
			var nextLocation = constrain ? CoerceLocation(value) : value;
			if (_location == nextLocation)
			{
				return;
			}

			var delta = nextLocation - _location;
			_location = nextLocation;
			_layoutState.SetLocation(Path, _location);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
			if (moveContents)
			{
				MoveContents(delta);
			}

			_parentBoundary?.RefreshMinimumSize();
		}

		private Point CoerceLocation(Point value)
		{
			if (_parentBoundary is null)
			{
				return value;
			}

			var minX = _parentBoundary.Location.X + BoundaryPaddingX;
			var minY = _parentBoundary.Location.Y + BoundaryPaddingTop;
			var maxX = _parentBoundary.Location.X + Math.Max(BoundaryPaddingX, _parentBoundary.Width - Width - BoundaryPaddingX);
			var maxY = _parentBoundary.Location.Y + Math.Max(BoundaryPaddingTop, _parentBoundary.Height - Height - BoundaryPaddingBottom);
			var result = new Point(Clamp(value.X, minX, maxX), Clamp(value.Y, minY, maxY));

			return result;
		}

		private void MoveContents(Vector delta)
		{
			foreach (var boundary in _directBoundaries)
			{
				boundary.MoveBy(delta, false);
			}

			foreach (var node in _directNodes)
			{
				node.MoveBy(delta, false);
			}
		}

		private Size CoerceSize(Size value)
		{
			var minimumSize = CalculateMinimumSize();
			var result = new Size(
				Math.Max(minimumSize.Width, value.Width),
				Math.Max(minimumSize.Height, value.Height));

			return result;
		}

		private Size CalculateMinimumSize()
		{
			var minimumWidth = NodeWidth + BoundaryPaddingX * 2;
			var minimumHeight = NodeHeight + BoundaryPaddingTop + BoundaryPaddingBottom;
			foreach (var node in _directNodes)
			{
				minimumWidth = Math.Max(minimumWidth, node.Location.X - Location.X + NodeWidth + BoundaryPaddingX);
				minimumHeight = Math.Max(minimumHeight, node.Location.Y - Location.Y + NodeHeight + BoundaryPaddingBottom);
			}

			foreach (var boundary in _directBoundaries)
			{
				minimumWidth = Math.Max(minimumWidth, boundary.Location.X - Location.X + boundary.Width + BoundaryPaddingX);
				minimumHeight = Math.Max(minimumHeight, boundary.Location.Y - Location.Y + boundary.Height + BoundaryPaddingBottom);
			}

			var result = new Size(minimumWidth, minimumHeight);

			return result;
		}

		private void NotifySizeChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualSize)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
			_layoutState.SetSize(Path, _actualSize);
			NotifyMinimumSizeChanged();
			_parentBoundary?.RefreshMinimumSize();
		}

		private void NotifyMinimumSizeChanged()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimumWidth)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimumHeight)));
		}
	}
}
