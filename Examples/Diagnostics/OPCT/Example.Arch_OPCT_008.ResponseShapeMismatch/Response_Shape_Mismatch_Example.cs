// ReSharper disable All - Justification: Example File

namespace Example.Arch_OPCT_008.ResponseShapeMismatch;

public sealed class PizzaKitchen
{
	// ARCH_OPCT_008: the configured owner returns the wrong response shape.
	public string PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		return "Order queued";
	}
}

public sealed class PlacePizzaOrderRequest;

public sealed class PlacePizzaOrderResponse;
