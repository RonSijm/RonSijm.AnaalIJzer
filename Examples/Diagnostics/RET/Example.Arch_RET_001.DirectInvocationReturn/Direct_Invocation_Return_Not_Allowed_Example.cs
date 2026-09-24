// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <ReturnValuePolicy description="The kitchen does not serve the result of an oven call directly.">
      <Invocation description="Assign the oven result to a named value before serving it." />
    </ReturnValuePolicy>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch_RET_001.DirectInvocationReturn;

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
	// ARCH_RET_001: method invocations may not be returned directly.
	public Pizza ServeOvenCallDirectly()
	{
		return oven.BakePizza();
	}

	// Valid: the invocation result is assigned before it is returned.
	public Pizza ServePreparedPizza()
	{
		var result = oven.BakePizza();

		return result;
	}

	// Valid: this policy does not confuse every non-identifier return with an invocation.
	public bool IsKitchenClosed()
	{
		return false;
	}

	// Valid: configure <Literal value="null" /> separately when null should be forbidden.
	public Pizza? FindDailySpecial()
	{
		return null;
	}
}
