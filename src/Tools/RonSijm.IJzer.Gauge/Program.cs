using RonSijm.IJzer.Gauge.CommandLine;

namespace RonSijm.IJzer.Gauge;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		using var cancellation = new CancellationTokenSource();
		ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			cancellation.Cancel();
		};
		Console.CancelKeyPress += cancelHandler;
		int result;
		try
		{
			result = await GaugeCommandLine.RunAsync(args, Console.Out, Console.Error, cancellation.Token);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
			result = 130;
		}
		finally
		{
			Console.CancelKeyPress -= cancelHandler;
		}

		return result;
	}
}
