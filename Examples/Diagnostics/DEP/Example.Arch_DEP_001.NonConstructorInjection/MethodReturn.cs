// ReSharper disable All - Justification: Example File
// ARCH_DEP_001: the customer exposes a chef through its own API.
namespace Example.Arch_DEP_001.NonConstructorInjection;

public class MethodReturnCustomer
{
    public IChef FindChef()
    {
        return null!;
    }
}