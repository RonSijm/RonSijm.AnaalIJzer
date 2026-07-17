// ReSharper disable All - Justification: Example File
// ARCH001: the customer exposes a chef through its own API.
namespace Example.Arch001.NonConstructorInjection;

public class MethodReturnCustomer
{
    public IChef FindChef()
    {
        return null!;
    }
}