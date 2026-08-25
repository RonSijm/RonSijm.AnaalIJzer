namespace RonSijm.AnaalIJzer.Exceptions;

public static class ArchitectureClock
{
	private static Func<DateTime> utcNowProvider = () => DateTime.UtcNow;

	public static DateTime UtcNow
	{
		get
		{
			var result = utcNowProvider();

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
		var previous = utcNowProvider;
		utcNowProvider = () => utcNow;

		var result = new FrozenClock(() => utcNowProvider = previous);

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
