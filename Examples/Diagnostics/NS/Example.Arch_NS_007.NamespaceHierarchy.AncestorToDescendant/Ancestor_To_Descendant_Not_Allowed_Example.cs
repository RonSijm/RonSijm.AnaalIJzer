// ReSharper disable All - Justification: Example File
namespace Restaurant
{
    // ARCH_NS_007: the root-level chef must not take a direct dependency on one feature's order ticket.
    public sealed class HeadChef(Orders.OrderTicket ticket)
    {
        public void Serve()
        {
            _ = ticket;
        }
    }
}

namespace Restaurant.Orders
{
    // Allowed: the feature namespace may depend on the root-level chef under this policy.
    public sealed class OrderTicket(Restaurant.HeadChef chef)
    {
        public void Deliver()
        {
            _ = chef;
        }
    }
}
