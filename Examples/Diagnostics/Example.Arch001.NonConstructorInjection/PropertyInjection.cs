// ReSharper disable All - Justification: Example File
// ARCH001: a settable property is still a direct Customer -> Chef dependency.
namespace Example.Arch001.NonConstructorInjection;

public class PropertyDependencyCustomer
{
    public IChef Chef { get; set; } = null!;
}