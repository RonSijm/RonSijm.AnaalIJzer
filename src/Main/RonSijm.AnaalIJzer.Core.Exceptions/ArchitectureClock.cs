namespace RonSijm.AnaalIJzer.Core.Exceptions;

public static class ArchitectureClock
{
	private static Func<DateTime> _utcNowProvider = () => DateTime.UtcNow;

	public static DateTime UtcNow
	{
		get
		{
			var result = _utcNowProvider();

			return result;
		}
	}

	public static DateTime UtcToday
	{
		get
		{
			var result = UtcNow.Date;

			return result;
		}
	}

	public static IDisposable Freeze(DateTime utcNow)
	{
		var previous = _utcNowProvider;
		_utcNowProvider = () => utcNow;

		var result = new FrozenClock(() => _utcNowProvider = previous);

		return result;
	}

	private sealed class FrozenClock(Action restore) : IDisposable
	{
		public void Dispose()
		{
			restore();
		}
	}
}
