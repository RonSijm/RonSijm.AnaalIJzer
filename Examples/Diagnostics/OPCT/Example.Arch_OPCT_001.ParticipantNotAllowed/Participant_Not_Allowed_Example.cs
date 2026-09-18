// ReSharper disable All - Justification: Example File

namespace Example.Arch_OPCT_001.ParticipantNotAllowed;

public sealed class PizzaKitchen
{
	// ARCH_OPCT_001: this owner belongs to Endpoint, not the allowed Application layer.
	public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
	{
		return new PlacePizzaOrderResponse();
	}
}

public sealed class PlacePizzaOrderRequest;

public sealed class PlacePizzaOrderResponse;
