// ReSharper disable All - Justification: Example File
namespace Restaurant
{
    // Allowed: the restaurant root may know about an order ticket.
    public sealed class HeadChef
    {
        public void Review(Orders.OrderTicket ticket)
        {
            _ = ticket;
        }
    }
}

namespace Restaurant.Orders
{
    // ARCH_NS_007: an order ticket must not reach back into the restaurant root for a chef.
    public sealed class OrderTicket(Restaurant.HeadChef chef)
    {
        public void Deliver()
        {
            _ = chef;
        }
    }
}
