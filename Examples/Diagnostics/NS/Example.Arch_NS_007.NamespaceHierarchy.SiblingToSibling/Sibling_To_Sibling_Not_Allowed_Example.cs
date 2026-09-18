// ReSharper disable All - Justification: Example File
namespace Restaurant
{
    public sealed class HeadChef
    {
    }
}

namespace Restaurant.Payments
{
    public sealed class PaymentLedger
    {
    }
}

namespace Restaurant.Orders
{
    // Allowed: an order feature may still use a root-level restaurant type.
    public sealed class OrderSupervisor(Restaurant.HeadChef chef)
    {
        public void Coordinate()
        {
            _ = chef;
        }
    }

    // ARCH_NS_007: Orders and Payments are sibling features, so the order ticket must not handle the payment ledger directly.
    public sealed class OrderTicket(Restaurant.Payments.PaymentLedger ledger)
    {
        public void Deliver()
        {
            _ = ledger;
        }
    }
}
