// ReSharper disable All - Justification: Example File

namespace Example.Arch023.OperationContract;

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
	// ARCH023: this selected waiter entry point never calls the configured kitchen owner.
	public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		return new PlacePizzaOrderResponse();
	}
}

public sealed class PizzaKitchen
{
	// ARCH023: the owner returns a raw kitchen status instead of the configured response contract.
	public string PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		return "Order queued";
	}
}

public sealed class PlacePizzaOrderRequest;

public sealed class PlacePizzaOrderResponse;
