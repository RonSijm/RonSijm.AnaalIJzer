// ReSharper disable All - Justification: Example File
// ARCH001: the customer retains a direct chef dependency in a field.
namespace Example.Arch001.NonConstructorInjection;

public class FieldDependencyCustomer
{
    private readonly IChef _chef = null!;
}