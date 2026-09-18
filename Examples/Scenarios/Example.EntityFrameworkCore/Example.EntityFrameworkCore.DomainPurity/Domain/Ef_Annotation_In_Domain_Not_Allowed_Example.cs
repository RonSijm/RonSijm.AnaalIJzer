// ReSharper disable All - Justification: Example File

using Microsoft.EntityFrameworkCore;

namespace Example.EntityFrameworkCore.DomainPurity.Domain;

// ARCH_DEP_001: Domain -> EfCoreAnnotation is blocked at Site=Attribute.
[Index(nameof(PizzaOrder.OrderNumber))]
public sealed class PizzaOrder
{
    public int Id { get; init; }

    public string OrderNumber { get; init; } = string.Empty;
}
