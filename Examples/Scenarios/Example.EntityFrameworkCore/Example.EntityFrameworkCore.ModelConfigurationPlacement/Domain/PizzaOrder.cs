// ReSharper disable All - Justification: Example File

namespace Example.EntityFrameworkCore.ModelConfigurationPlacement.Domain;

public sealed class PizzaOrder
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
