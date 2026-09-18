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
	// ARCH_RET_001: an oven call goes straight to the waiter without a named hand-off.
	public Pizza ServePizzaWithoutANamedResult()
	{
		return oven.BakePizza();
	}

	// Valid: the named result is where the kitchen could inspect or normalize the pizza.
	public Pizza ServePizzaWithANamedResult()
	{
		var result = oven.BakePizza();

		return result;
	}
}
