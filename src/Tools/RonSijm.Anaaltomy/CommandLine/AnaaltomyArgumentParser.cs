namespace RonSijm.Anaaltomy.CommandLine;

internal static class AnaaltomyArgumentParser
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

	public static AnaaltomyOptions Parse(IReadOnlyList<string> arguments)
	{
		if (arguments.Count == 0 || arguments[0] is "--help" or "-h" or "help")
		{
			var helpOptions = new AnaaltomyOptions(AnaaltomyCommand.Help, new Dictionary<string, string?>(), new HashSet<string>());

			return helpOptions;
		}

		if (!TryParseCommand(arguments[0], out var command))
		{
			throw new ArgumentException("Unknown Anaaltomy command: " + arguments[0]);
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

			return new AnaaltomyOptions(AnaaltomyCommand.Help, new Dictionary<string, string?>(), new HashSet<string>());
		}

		ValidateOptions(command, values.Keys, flags);
		var result = new AnaaltomyOptions(command, values, flags);

		return result;
	}

	private static bool TryParseCommand(string value, out AnaaltomyCommand command)
	{
		var result = value.ToLowerInvariant() switch
		{
			"scan" => AnaaltomyCommand.Scan,
			"history" => AnaaltomyCommand.History,
			"summary" => AnaaltomyCommand.Summary,
			"trend" => AnaaltomyCommand.Trend,
			"compare" => AnaaltomyCommand.Compare,
			"commits" => AnaaltomyCommand.Commits,
			"export" => AnaaltomyCommand.Export,
			"chart" => AnaaltomyCommand.Chart,
			_ => AnaaltomyCommand.Help
		};
		command = result;
		var isKnown = value.Equals("scan", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("history", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("summary", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("trend", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("compare", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("commits", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("export", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("chart", StringComparison.OrdinalIgnoreCase);

		return isKnown;
	}

	private static void ValidateOptions(AnaaltomyCommand command, IEnumerable<string> valueNames, IEnumerable<string> flagNames)
	{
		var allowedValueNames = command switch
		{
			AnaaltomyCommand.Scan => new[] { "--project", "--solution", "--directory", "--database", "--configuration", "--framework", "--restore-mode" },
			AnaaltomyCommand.History => new[] { "--repository", "--database", "--configuration", "--framework", "--restore-mode", "--from", "--to", "--max-commits" },
			AnaaltomyCommand.Summary => new[] { "--database" },
			AnaaltomyCommand.Trend => new[] { "--database", "--dimension", "--bucket" },
			AnaaltomyCommand.Compare => new[] { "--database", "--from", "--to" },
			AnaaltomyCommand.Commits => new[] { "--database", "--dimension", "--bucket" },
			AnaaltomyCommand.Export => new[] { "--database", "--format", "--output" },
			AnaaltomyCommand.Chart => new[] { "--database", "--output-directory", "--dimension", "--bucket", "--group-by" },
			_ => []
		};
		var allowedFlagNames = command switch
		{
			AnaaltomyCommand.Scan => new[] { "--include-generated", "--allow-partial" },
			AnaaltomyCommand.History => new[] { "--from-root", "--first-parent", "--resume", "--allow-partial", "--include-generated", "--include-commit-metadata" },
			AnaaltomyCommand.Chart => new[] { "--trend", "--group" },
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
