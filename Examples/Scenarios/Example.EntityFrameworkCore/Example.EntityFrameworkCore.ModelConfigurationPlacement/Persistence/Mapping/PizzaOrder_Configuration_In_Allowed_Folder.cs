// ReSharper disable All - Justification: Example File

using Example.EntityFrameworkCore.ModelConfigurationPlacement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Example.EntityFrameworkCore.ModelConfigurationPlacement.Persistence.Mapping;

public sealed class PizzaOrderConfiguration : IEntityTypeConfiguration<PizzaOrder>
{
    public void Configure(EntityTypeBuilder<PizzaOrder> builder)
    {
        builder.HasKey(order => order.Id);
    }
}
