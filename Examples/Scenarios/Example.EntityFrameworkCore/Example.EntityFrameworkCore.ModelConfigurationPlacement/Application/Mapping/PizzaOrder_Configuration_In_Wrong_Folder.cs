// ReSharper disable All - Justification: Example File

using Example.EntityFrameworkCore.ModelConfigurationPlacement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Example.EntityFrameworkCore.ModelConfigurationPlacement.Application.Mapping;

// ARCH015: an IEntityTypeConfiguration implementation belongs in Persistence/Mapping.
public sealed class PreviewPizzaOrderConfiguration : IEntityTypeConfiguration<PizzaOrder>
{
    public void Configure(EntityTypeBuilder<PizzaOrder> builder)
    {
        builder.HasKey(order => order.Id);
    }
}
