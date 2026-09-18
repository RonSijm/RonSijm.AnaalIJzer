// ReSharper disable All - Justification: Example File
// ARCH_DEP_001: moving the chef dependency to a method does not restore the waiter.
namespace Example.Arch_DEP_001.NonConstructorInjection;

public class MethodDependencyCustomer
{
    public void OrderFrom(IChef chef) { }
}