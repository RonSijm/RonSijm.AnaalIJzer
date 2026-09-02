// ReSharper disable All - Justification: Example File

#nullable disable

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <ReturnValuePolicy description="A kitchen either serves a pizza or says how it could not.">
      <Literal value="null" description="A silent empty plate is not a serving decision." />
    </ReturnValuePolicy>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch020.ExplicitNullReturn;

public sealed class Pizza { }

public sealed class PizzaKitchen
{
	// Valid: the kitchen serves an actual pizza.
	public Pizza PrepareMargherita()
	{
		return new Pizza();
	}

	// ARCH020: a kitchen cannot silently hand the waiter nothing.
	public Pizza PrepareMysteryPizza()
	{
		return null;
	}
}
