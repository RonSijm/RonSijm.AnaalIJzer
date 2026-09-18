// ReSharper disable All - Justification: Example File

namespace Example.Arch_OPCT_002.RequiredOwnerInvocation;

public sealed class PizzaOrderController(PizzaKitchen kitchen)
{
	// Valid entry point: the waiter delegates directly to the configured kitchen owner.
	public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		_ = kitchen.PlacePizzaOrder(request);

		return new PlacePizzaOrderResponse();
	}
}

public sealed class PizzaOrderPreviewController
{
	// ARCH_OPCT_002: this selected waiter entry point never calls the configured kitchen owner.
	public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		return new PlacePizzaOrderResponse();
	}
}

public sealed class PizzaKitchen
{
	public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		return new PlacePizzaOrderResponse();
	}
}

public sealed class PlacePizzaOrderRequest;

public sealed class PlacePizzaOrderResponse;
