// ReSharper disable All - Justification: Example File

namespace Example.GlobalReturnValuePolicy;

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

	// Valid: the rule forbids invocations, not literals.
	public bool IsKitchenClosed()
	{
		return false;
	}

	// Valid: null is not an invocation either. Use a separate Literal rule to forbid it.
	public Pizza? FindDailySpecial()
	{
		return null;
	}
}
