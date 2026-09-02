using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.Arse;

internal static partial class ArseCommandLine
{
	private static bool IsHelp(string value)
	{
		var result = value is "-h" or "--help" or "help";

		return result;
	}

	private static void PrintHelp()
	{
		Console.WriteLine("Arse");
		Console.WriteLine("Architecture Rules, Settings, and Evidence");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  arse                  Open the interactive terminal interface.");
		Console.WriteLine("  arse tui              Explicitly open the interactive terminal interface.");
		Console.WriteLine("  arse associate-anl    Associate .anl files with Arse for the current Windows user.");
		Console.WriteLine("  arse unassociate-anl  Remove Arse's .anl file association for the current Windows user.");
		foreach (var operation in ApplicationOperationCatalog.All)
		{
			Console.WriteLine($"  arse {operation.Usage}");
		}

		Console.WriteLine();
		Console.WriteLine("Options:");
		foreach (var input in ApplicationInputCatalog.All)
		{
			var optionNames = input.ShortOption is null ? input.OptionName : $"{input.OptionName}, {input.ShortOption}";
			Console.WriteLine($"  {optionNames,-23}{input.Description}");
		}

		Console.WriteLine("  --output, -o           Output file or directory. Defaults to an input-local path.");
		Console.WriteLine("  --fix-id               Configuration fix id to apply with apply-fix.");
		Console.WriteLine("  --configuration, -c    MSBuild configuration. Defaults to Release.");
		Console.WriteLine("  --strategy             Config generation: snapshot, helpful, or conventions. Defaults to snapshot.");
		Console.WriteLine("  --minimum-confidence   Convention confidence from 0 to 1. Defaults to 0.90.");
		Console.WriteLine("  --minimum-support      Convention caller count. Defaults to 5.");
		Console.WriteLine("  --include-code-evidence Include rule matches and violations in project documentation.");
		Console.WriteLine("  --include-input        Include the input architecture settings in generated documentation.");
		Console.WriteLine("  --generate-documentation Generate code-backed documentation with a new configuration.");
		Console.WriteLine("  --force, -f            Overwrite an existing output file.");

		var aliasedOperations = ApplicationOperationCatalog.All.Where(operation => operation.Aliases.Count > 0).ToArray();
		if (aliasedOperations.Length == 0)
		{
			return;
		}

		Console.WriteLine();
		Console.WriteLine("Aliases:");
		foreach (var operation in aliasedOperations)
		{
			Console.WriteLine($"  {string.Join(", ", operation.Aliases)} -> {operation.CommandName}");
		}
	}
}
