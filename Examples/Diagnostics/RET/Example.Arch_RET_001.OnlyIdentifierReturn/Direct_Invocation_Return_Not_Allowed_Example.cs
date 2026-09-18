// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <ReturnValuePolicy description="The kitchen makes its serving decision before handing a pizza to the waiter.">
      <AllowedReturn description="A prepared pizza leaves through a named hand-off point.">
        <Identifier />
      </AllowedReturn>
    </ReturnValuePolicy>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch_RET_001.OnlyIdentifierReturn;

public sealed class Pizza { }

public sealed class PizzaOven
{
	public Pizza BakePizza()
	{
		var result = new Pizza();

		return result;
	}
}

public sealed class PizzaKitchen(PizzaOven oven)
{
	// ARCH_RET_001: the kitchen hands the waiter an unfinished oven call directly.
	public Pizza PreparePizzaTheHardToInspectWay()
	{
		return oven.BakePizza();
	}

	// Valid: the named result is an intentional hand-off point for inspection or handling.
	public Pizza PreparePizzaWithAResult()
	{
		var result = oven.BakePizza();

		return result;
	}
}
