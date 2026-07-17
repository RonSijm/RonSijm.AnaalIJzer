using System.Globalization;
using RonSijm.AnaalIJzer.Arse.FileExtension;
using RonSijm.AnaalIJzer.Tooling;

namespace RonSijm.AnaalIJzer.Arse;

internal static class ArseCommandLine
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

			var operation = ToolOperationCatalog.Find(args[0]) ?? throw new CommandLineException($"Unknown command: {args[0]}");
			var options = CommandOptions.Parse(args.Skip(1).ToArray());
			var request = options.ToRequest(operation.Kind);
			var result = await new ToolRunner().ExecuteAsync(request);
			Console.WriteLine(result.Message);
			return result.HasFindings ? 3 : 0;
		}
		catch (CommandLineException ex)
		{
			Console.Error.WriteLine(ex.Message);
			PrintHelp();
			return 2;
		}
		catch (ToolingException ex)
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
		foreach (var operation in ToolOperationCatalog.All)
		{
			Console.WriteLine($"  arse {operation.Usage}");
		}

		Console.WriteLine();
		Console.WriteLine("Options:");
		foreach (var input in ToolInputCatalog.All)
		{
			var optionNames = input.ShortOption is null ? input.OptionName : $"{input.OptionName}, {input.ShortOption}";
			Console.WriteLine($"  {optionNames,-23}{input.Description}");
		}

		Console.WriteLine("  --output, -o           Output file or directory. Defaults to an input-local path.");
		Console.WriteLine("  --configuration, -c    MSBuild configuration. Defaults to Release.");
		Console.WriteLine("  --strategy             Config generation: snapshot, helpful, or conventions. Defaults to snapshot.");
		Console.WriteLine("  --minimum-confidence   Convention confidence from 0 to 1. Defaults to 0.90.");
		Console.WriteLine("  --minimum-support      Convention caller count. Defaults to 5.");
		Console.WriteLine("  --include-code-evidence Include rule matches and violations in project documentation.");
		Console.WriteLine("  --include-input        Include the input architecture XML in generated documentation.");
		Console.WriteLine("  --generate-documentation Generate code-backed documentation with a new configuration.");
		Console.WriteLine("  --force, -f            Overwrite an existing output file.");

		var aliasedOperations = ToolOperationCatalog.All.Where(operation => operation.Aliases.Count > 0).ToArray();
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

	private static bool TryRunFileAssociationCommand(string commandName, out FileAssociationResult result)
	{
		if (string.Equals(commandName, "associate-anl", StringComparison.OrdinalIgnoreCase))
		{
			result = ArseFileAssociation.AssociateAnlFiles();
			return true;
		}

		if (string.Equals(commandName, "unassociate-anl", StringComparison.OrdinalIgnoreCase))
		{
			result = ArseFileAssociation.UnassociateAnlFiles();
			return true;
		}

		result = new FileAssociationResult(false, string.Empty);
		return false;
	}
}

internal sealed record CommandOptions
{
	public ToolInputKind? InputKind { get; private init; }
	public IReadOnlyList<string> InputPaths { get; private init; } = [];
	public string? OutputPath { get; private init; }
	public string Configuration { get; private init; } = "Release";
	public ConfigurationGenerationOptions GenerationOptions { get; private init; } = new();
	public bool IncludeCodeEvidence { get; private init; }
	public bool IncludeDocumentationInput { get; private init; }
	public bool GenerateDocumentation { get; private init; }
	public bool Force { get; private init; }

	public static CommandOptions Parse(string[] args)
	{
		var options = new CommandOptions();
		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			switch (arg)
			{
				case "--output":
				case "-o":
					options = options with { OutputPath = ReadValue(args, ref i, arg) };
					break;
				case "--configuration":
				case "-c":
					options = options with { Configuration = ReadValue(args, ref i, arg) };
					break;
				case "--strategy":
					options = options with
					{
						GenerationOptions = options.GenerationOptions with { Strategy = ParseStrategy(ReadValue(args, ref i, arg)) }
					};
					break;
				case "--minimum-confidence":
					options = options with
					{
						GenerationOptions = options.GenerationOptions with { MinimumConfidence = ParseConfidence(ReadValue(args, ref i, arg)) }
					};
					break;
				case "--minimum-support":
					options = options with
					{
						GenerationOptions = options.GenerationOptions with { MinimumSupport = ParseSupport(ReadValue(args, ref i, arg)) }
					};
					break;
				case "--include-code-evidence":
					options = options with { IncludeCodeEvidence = true };
					break;
				case "--include-input":
					options = options with { IncludeDocumentationInput = true };
					break;
				case "--generate-documentation":
					options = options with { GenerateDocumentation = true };
					break;
				case "--force":
				case "-f":
					options = options with { Force = true };
					break;
				default:
					var input = ToolInputCatalog.FindOption(arg);
					if (input is null)
					{
						throw new CommandLineException($"Unknown option: {arg}");
					}

					if (options.InputKind is not null && options.InputKind != input.Kind)
					{
						throw new CommandLineException("Use only one input option.");
					}

					options = options with
					{
						InputKind = input.Kind,
						InputPaths = [..options.InputPaths, ReadValue(args, ref i, arg)]
					};
					break;
			}
		}

		return options;
	}

	public ToolRequest ToRequest(ToolOperationKind operation)
	{
		return new ToolRequest(operation)
		{
			InputKind = InputKind,
			InputPaths = InputPaths,
			OutputPath = OutputPath,
			Configuration = Configuration,
			GenerationOptions = GenerationOptions,
			IncludeCodeEvidence = IncludeCodeEvidence,
			IncludeDocumentationInput = IncludeDocumentationInput,
			GenerateDocumentation = GenerateDocumentation,
			Force = Force
		};
	}

	private static ConfigurationGenerationStrategy ParseStrategy(string value)
	{
		if (Enum.TryParse<ConfigurationGenerationStrategy>(value, true, out var strategy))
		{
			return strategy;
		}

		throw new CommandLineException($"Unknown generation strategy: {value}. Use snapshot, helpful, or conventions.");
	}

	private static double ParseConfidence(string value)
	{
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence))
		{
			return confidence;
		}

		throw new CommandLineException($"Invalid minimum confidence: {value}. Use a number from 0 to 1.");
	}

	private static int ParseSupport(string value)
	{
		if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var support))
		{
			return support;
		}

		throw new CommandLineException($"Invalid minimum support: {value}. Use a whole number.");
	}

	private static string ReadValue(string[] args, ref int index, string optionName)
	{
		if (index + 1 >= args.Length)
		{
			throw new CommandLineException($"Missing value for {optionName}.");
		}

		return args[++index];
	}
}

internal sealed class CommandLineException(string message) : Exception(message);
