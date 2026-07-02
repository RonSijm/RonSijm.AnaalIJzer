using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorConsole.Core;
using RonSijm.AnaalIJzer.Tooling;
using RonSijm.AnaalIJzer.Ridder.Components;

namespace RonSijm.AnaalIJzer.Ridder;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		if (args.Length > 0 && !string.Equals(args[0], "tui", StringComparison.OrdinalIgnoreCase))
		{
			return await RidderCommandLine.RunAsync(args);
		}

		if (args.Length == 0 && (Console.IsInputRedirected || Console.IsOutputRedirected))
		{
			return await RidderCommandLine.RunAsync(["--help"]);
		}

		var builder = Host.CreateDefaultBuilder(args.Skip(1).ToArray())
			.UseRazorConsole<App>(configure: configuration =>
			{
				configuration.ConfigureServices(services => services.AddSingleton<ToolRunner>());
			});

		await builder.Build().RunAsync();
		return 0;
	}
}
