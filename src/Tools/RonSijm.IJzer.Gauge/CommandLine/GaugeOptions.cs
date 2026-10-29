namespace RonSijm.IJzer.Gauge.CommandLine;

internal sealed class GaugeOptions(GaugeCommand command, IReadOnlyDictionary<string, string?> values, IReadOnlySet<string> flags)
{
	public GaugeCommand Command { get; } = command;
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
