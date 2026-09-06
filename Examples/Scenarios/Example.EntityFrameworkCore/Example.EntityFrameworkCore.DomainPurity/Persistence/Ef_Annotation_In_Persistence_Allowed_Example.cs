// ReSharper disable All - Justification: Example File

using Microsoft.EntityFrameworkCore;

namespace Example.EntityFrameworkCore.DomainPurity.Persistence;

[Index(nameof(PersistedPizzaOrder.OrderNumber))]
public sealed class PersistedPizzaOrder
{
    public int Id { get; init; }

    public string OrderNumber { get; init; } = string.Empty;
}
