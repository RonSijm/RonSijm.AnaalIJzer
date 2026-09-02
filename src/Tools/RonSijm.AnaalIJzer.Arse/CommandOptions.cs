using System.Globalization;
using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.Arse;

internal sealed record CommandOptions
{
	private ApplicationInputKind? InputKind { get; init; }
	private IReadOnlyList<string> InputPaths { get; init; } = [];
	private string? OutputPath { get; init; }
	private string? FixId { get; init; }
	private string Configuration { get; init; } = "Release";
	private ConfigurationGenerationOptions GenerationOptions { get; init; } = new();
	private bool IncludeCodeEvidence { get; init; }
	private bool IncludeDocumentationInput { get; init; }
	private bool GenerateDocumentation { get; init; }
	private bool Force { get; init; }

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
				case "--fix-id":
					options = options with { FixId = ReadValue(args, ref i, arg) };
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
					var input = ApplicationInputCatalog.FindOption(arg);
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

	public ApplicationRequest ToRequest(ApplicationOperationKind operation)
	{
		return new ApplicationRequest(operation)
		{
			InputKind = InputKind,
			InputPaths = InputPaths,
			OutputPath = OutputPath,
			FixId = FixId,
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
