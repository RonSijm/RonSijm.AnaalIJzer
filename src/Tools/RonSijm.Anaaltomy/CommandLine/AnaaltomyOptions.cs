namespace RonSijm.Anaaltomy.CommandLine;

internal sealed class AnaaltomyOptions(AnaaltomyCommand command, IReadOnlyDictionary<string, string?> values, IReadOnlySet<string> flags)
{
	public AnaaltomyCommand Command { get; } = command;
	public IReadOnlyDictionary<string, string?> Values { get; } = values;
	public IReadOnlySet<string> Flags { get; } = flags;

	public string? GetValue(string name)
	{
		Values.TryGetValue(name, out var result);

		return result;
	}

	public bool HasFlag(string name)
	{
		var result = Flags.Contains(name);

		return result;
	}
}
