// ReSharper disable All - Justification: Example File
// ARCH001: creating a chef directly still bypasses the waiter.
namespace Example.Arch001.NonConstructorInjection;

public class NewingCustomer
{
    public void Run()
    {
        var chef = new DirectChef();
        _ = chef;
    }
}