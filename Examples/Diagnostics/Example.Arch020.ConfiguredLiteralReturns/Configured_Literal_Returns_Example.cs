// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <ReturnValuePolicy description="A kitchen does not use sentinel values as a serving decision.">
      <Literal value="" description="An empty menu name hides the missing decision." />
      <Literal value="42" description="The special slice count is not a magic fallback." />
      <Literal value="0" description="No pizza status must be named explicitly." />
    </ReturnValuePolicy>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch020.ConfiguredLiteralReturns;

public enum PizzaStatus
{
	None,
	Ready
}

public sealed class PizzaKitchen
{
	// ARCH020: an empty name is a missing menu decision.
	public string GetFeaturedPizzaName()
	{
		return "";
	}

	// ARCH020: 42 is configured as a forbidden slice-count sentinel.
	public int GetFeaturedSliceCount()
	{
		return 42;
	}

	// ARCH020: enum zero is still a literal return after the enum cast is unwrapped.
	public PizzaStatus GetFeaturedPizzaStatus()
	{
		return (PizzaStatus)0;
	}

	// Valid: a real status is returned.
	public PizzaStatus GetReadyPizzaStatus()
	{
		return PizzaStatus.Ready;
	}
}
