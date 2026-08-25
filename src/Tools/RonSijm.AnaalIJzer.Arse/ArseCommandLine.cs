using System.Globalization;
using RonSijm.AnaalIJzer.Arse.FileExtension;
using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.Arse;

internal static partial class ArseCommandLine
{
	public static async Task<int> RunAsync(string[] args)
	{
		if (args.Length == 0 || IsHelp(args[0]))
		{
			PrintHelp();
			return 0;
		}

		try
		{
			if (TryRunFileAssociationCommand(args[0], out var associationResult))
			{
				Console.WriteLine(associationResult.Message);
				return 0;
			}

			var operation = ApplicationOperationCatalog.Find(args[0]) ?? throw new CommandLineException($"Unknown command: {args[0]}");
			var options = CommandOptions.Parse(args.Skip(1).ToArray());
			var request = options.ToRequest(operation.Kind);
			var result = await new ApplicationRunner().ExecuteAsync(request);
			Console.WriteLine(result.Message);
			return result.HasFindings ? 3 : 0;
		}
		catch (CommandLineException ex)
		{
			Console.Error.WriteLine(ex.Message);
			PrintHelp();
			return 2;
		}
		catch (ApplicationOperationException ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 2;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 1;
		}
	}
}
