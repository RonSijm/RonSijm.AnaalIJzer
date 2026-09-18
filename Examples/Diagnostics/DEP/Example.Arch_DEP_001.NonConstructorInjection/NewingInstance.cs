// ReSharper disable All - Justification: Example File
// ARCH_DEP_001: creating a chef directly still bypasses the waiter.
namespace Example.Arch_DEP_001.NonConstructorInjection;

public class NewingCustomer
{
    public void Run()
    {
        var chef = new DirectChef();
        _ = chef;
    }
}