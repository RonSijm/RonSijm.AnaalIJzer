using Xunit;

namespace RonSijm.AnaalIJzer.Testing;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ArchitectureClockTestCollection
{
	public const string Name = "ArchitectureClock";
}
