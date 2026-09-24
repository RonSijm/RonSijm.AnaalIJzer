using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorConsole.Core;
using RonSijm.AnaalIJzer.Application;
using RonSijm.AnaalIJzer.Arse.Components;

namespace RonSijm.AnaalIJzer.Arse;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && !string.Equals(args[0], "tui", StringComparison.OrdinalIgnoreCase))
        {
            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
            try
            {
                return await ArseCommandLine.RunAsync(args, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return 130;
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        if (args.Length == 0 && (Console.IsInputRedirected || Console.IsOutputRedirected))
        {
            return await ArseCommandLine.RunAsync(["--help"], CancellationToken.None);
        }

        var builder = Host.CreateDefaultBuilder([.. args.Skip(1)])
            .UseRazorConsole<App>(configure: configuration =>
            {
                configuration.ConfigureServices(services => services.AddSingleton<ApplicationRunner>());
            });

        await builder.Build().RunAsync();
        return 0;
    }
}