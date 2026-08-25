using RonSijm.AnaalIJzer.Outputs.GraphExports;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class GraphImageExportCommand
{
	public static bool TryCreate(string[] args, out GraphImageExportCommand? command, out string? error)
	{
		command = null;
		error = null;
		var exportIndex = IndexOf(args, "--export");
		var examplesIndex = IndexOf(args, "--export-examples");
		if (exportIndex < 0 && examplesIndex < 0)
		{
			return false;
		}

		if (exportIndex >= 0 && examplesIndex >= 0)
		{
			error = "Use either --export or --export-examples, not both.";

			return true;
		}

		var configuration = GetOptionValue(args, "--configuration") ?? "Release";
		var width = GetIntOptionValue(args, "--width", DefaultWidth);
		var height = GetIntOptionValue(args, "--height", DefaultHeight);
		var failOnError = HasOption(args, "--fail-on-error");
		if (width <= 0 || height <= 0)
		{
			error = "--width and --height must be positive numbers.";

			return true;
		}

		if (exportIndex >= 0)
		{
			if (!TryGetPositionalPair(args, exportIndex, out var input, out var output, out error))
			{
				return true;
			}

			command = new GraphImageExportCommand(ArchitectureGraphImageExportMode.Single, input, output, configuration, width, height, failOnError);

			return true;
		}

		if (!TryGetPositionalPair(args, examplesIndex, out var examplesRoot, out var outputDirectory, out error))
		{
			return true;
		}

		command = new GraphImageExportCommand(ArchitectureGraphImageExportMode.Examples, examplesRoot, outputDirectory, configuration, width, height, failOnError);

		return true;
	}

	private static bool TryGetPositionalPair(string[] args, int commandIndex, out string first, out string second, out string? error)
	{
		first = string.Empty;
		second = string.Empty;
		error = null;
		if (commandIndex + 2 >= args.Length)
		{
			error = args[commandIndex] + " expects an input path and an output path.";

			return false;
		}

		first = args[commandIndex + 1];
		second = args[commandIndex + 2];
		if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
		{
			error = args[commandIndex] + " expects non-empty input and output paths.";

			return false;
		}

		return true;
	}

	private static string? GetOptionValue(string[] args, string option)
	{
		var index = IndexOf(args, option);
		if (index < 0 || index + 1 >= args.Length)
		{
			return null;
		}

		var result = args[index + 1];

		return result;
	}

	private static int GetIntOptionValue(string[] args, string option, int fallback)
	{
		var value = GetOptionValue(args, option);
		var result = int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: fallback;

		return result;
	}

	private static bool HasOption(string[] args, string option)
	{
		var result = IndexOf(args, option) >= 0;

		return result;
	}

	private static int IndexOf(string[] args, string option)
	{
		for (var index = 0; index < args.Length; index++)
		{
			if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
			{
				return index;
			}
		}

		return -1;
	}
}
