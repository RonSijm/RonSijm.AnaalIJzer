namespace ExampleCompany.Billing.Contracts
{
	public class InvoiceContract { }
}

namespace ExampleCompany.Ordering.Repository
{
	public class OrderRepository { }
}

namespace ExampleCompany.Ordering.Application
{
	// Allowed inside Ordering: Application -> Repository.
	// Allowed across boundaries: Ordering -> Billing, Application egress, then Contracts ingress.
	public class PlaceOrderService(ExampleCompany.Ordering.Repository.OrderRepository repository, ExampleCompany.Billing.Contracts.InvoiceContract invoice) { }
}

namespace ExampleCompany.Ordering
{
	// A type that matches the parent scope but no child belongs directly to Ordering.
	public class OrderModule(ExampleCompany.Ordering.Repository.OrderRepository repository) { }
}

namespace ExampleCompany.Shipping.Contracts
{
	public class ShippingContract { }
}

namespace ExampleCompany.Catalog.Application
{
	// ARCH001: the root Catalog -> Shipping gate is missing.
	public class CatalogService(ExampleCompany.Shipping.Contracts.ShippingContract contract) { }
}

namespace ExampleCompany.Support.Contracts
{
	public class SupportContract { }
}

namespace ExampleCompany.Fulfillment.Application
{
	// ARCH001: Fulfillment/Application has no egress gate to Support/Contracts.
	public class FulfillmentService(ExampleCompany.Support.Contracts.SupportContract contract) { }
}

namespace ExampleCompany.Payments.Contracts
{
	public class PaymentContract { }
}

namespace ExampleCompany.Inventory.Application
{
	// ARCH001: Payments has no ingress gate into Contracts.
	public class InventoryService(ExampleCompany.Payments.Contracts.PaymentContract contract) { }
}
