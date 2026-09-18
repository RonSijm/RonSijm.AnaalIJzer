// ReSharper disable All - Justification: Example File
namespace Restaurant.Orders
{
    public sealed class PizzaMenu
    {
    }

    // ARCH_NS_007: this rule prevents one Orders type from directly depending on another Orders type.
    public sealed class OrderTicket(PizzaMenu menu)
    {
        public void Deliver()
        {
            _ = menu;
        }
    }
}

namespace Restaurant
{
    // Allowed: moving from the restaurant root into Orders is not a same-namespace dependency.
    public sealed class HeadChef(Orders.OrderTicket ticket)
    {
        public void Serve()
        {
            _ = ticket;
        }
    }
}
