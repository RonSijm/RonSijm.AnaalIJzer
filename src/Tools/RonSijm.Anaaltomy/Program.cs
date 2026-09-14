using RonSijm.Anaaltomy.CommandLine;

namespace RonSijm.Anaaltomy;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		var result = await AnaaltomyCommandLine.RunAsync(args, Console.Out, Console.Error, CancellationToken.None);

		return result;
	}
}
