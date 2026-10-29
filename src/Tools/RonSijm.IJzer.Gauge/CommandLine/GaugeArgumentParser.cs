namespace RonSijm.IJzer.Gauge.CommandLine;

internal static class GaugeArgumentParser
{
	private static readonly HashSet<string> FlagNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"--from-root",
		"--first-parent",
		"--resume",
		"--allow-partial",
		"--include-generated",
		"--include-commit-metadata",
		"--trend",
		"--group",
		"--help",
		"-h"
	};

	public static GaugeOptions Parse(IReadOnlyList<string> arguments)
	{
		if (arguments.Count == 0 || arguments[0] is "--help" or "-h" or "help")
		{
			var helpOptions = new GaugeOptions(GaugeCommand.Help, new Dictionary<string, string?>(), new HashSet<string>());

			return helpOptions;
		}

		if (!TryParseCommand(arguments[0], out var command))
		{
			throw new ArgumentException("Unknown IJzer Gauge command: " + arguments[0]);
		}

		var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (var index = 1; index < arguments.Count; index++)
		{
			var argument = arguments[index];
			if (!argument.StartsWith("--", StringComparison.Ordinal))
			{
				throw new ArgumentException("Unexpected argument: " + argument);
			}

			if (FlagNames.Contains(argument))
			{
				if (!flags.Add(argument))
				{
					throw new ArgumentException("Option was supplied more than once: " + argument);
				}

				continue;
			}

			if (index + 1 >= arguments.Count || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
			{
				throw new ArgumentException("Option requires a value: " + argument);
			}

			if (!values.TryAdd(argument, arguments[++index]))
			{
				throw new ArgumentException("Option was supplied more than once: " + argument);
			}
		}

		if (flags.Contains("--help") || flags.Contains("-h"))
		{
			if (values.Count > 0 || flags.Count > 1)
			{
				throw new ArgumentException("--help cannot be combined with other options.");
			}

			return new GaugeOptions(GaugeCommand.Help, new Dictionary<string, string?>(), new HashSet<string>());
		}

		ValidateOptions(command, values.Keys, flags);
		var result = new GaugeOptions(command, values, flags);

		return result;
	}

	private static bool TryParseCommand(string value, out GaugeCommand command)
	{
		var result = value.ToLowerInvariant() switch
		{
			"scan" => GaugeCommand.Scan,
			"history" => GaugeCommand.History,
			"summary" => GaugeCommand.Summary,
			"trend" => GaugeCommand.Trend,
			"compare" => GaugeCommand.Compare,
			"commits" => GaugeCommand.Commits,
			"export" => GaugeCommand.Export,
			"export-database" => GaugeCommand.ExportDatabase,
			"chart" => GaugeCommand.Chart,
			_ => GaugeCommand.Help
		};
		command = result;
		var isKnown = value.Equals("scan", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("history", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("summary", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("trend", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("compare", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("commits", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("export", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("export-database", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("chart", StringComparison.OrdinalIgnoreCase);

		return isKnown;
	}

	private static void ValidateOptions(GaugeCommand command, IEnumerable<string> valueNames, IEnumerable<string> flagNames)
	{
		var allowedValueNames = command switch
		{
			GaugeCommand.Scan => new[] { "--project", "--solution", "--directory", "--database", "--configuration", "--framework", "--restore-mode" },
			GaugeCommand.History => new[] { "--repository", "--database", "--configuration", "--framework", "--restore-mode", "--from", "--to", "--max-commits" },
			GaugeCommand.Summary => new[] { "--database" },
			GaugeCommand.Trend => new[] { "--database", "--dimension", "--bucket" },
			GaugeCommand.Compare => new[] { "--database", "--from", "--to" },
			GaugeCommand.Commits => new[] { "--database", "--dimension", "--bucket" },
			GaugeCommand.Export => new[] { "--database", "--format", "--output" },
			GaugeCommand.ExportDatabase => new[] { "--database", "--format", "--output-directory" },
			GaugeCommand.Chart => new[] { "--database", "--output-directory", "--dimension", "--bucket", "--group-by" },
			_ => []
		};
		var allowedFlagNames = command switch
		{
			GaugeCommand.Scan => new[] { "--include-generated", "--allow-partial" },
			GaugeCommand.History => new[] { "--from-root", "--first-parent", "--resume", "--allow-partial", "--include-generated", "--include-commit-metadata" },
			GaugeCommand.Chart => new[] { "--trend", "--group" },
			_ => []
		};
		var invalidValueName = valueNames.FirstOrDefault(valueName => !allowedValueNames.Contains(valueName, StringComparer.OrdinalIgnoreCase));
		if (invalidValueName is not null)
		{
			throw new ArgumentException("Option is not valid for '" + command.ToString().ToLowerInvariant() + "': " + invalidValueName);
		}

		var invalidFlagName = flagNames.FirstOrDefault(flagName => !allowedFlagNames.Contains(flagName, StringComparer.OrdinalIgnoreCase));
		if (invalidFlagName is not null)
		{
			throw new ArgumentException("Option is not valid for '" + command.ToString().ToLowerInvariant() + "': " + invalidFlagName);
		}
	}
}
