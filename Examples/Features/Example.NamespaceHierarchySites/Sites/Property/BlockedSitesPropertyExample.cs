// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedProperty
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedProperty.Requests
{
    public sealed class BlockedSitesPropertyExample
    {
        // Property is excluded from this block by blockedSites.
        public Restaurant.NamespaceHierarchySites.BlockedProperty.PantryIngredient Ingredient { get; } = null!;

        // ARCH_NS_007: a constructor remains blocked.
        public BlockedSitesPropertyExample(Restaurant.NamespaceHierarchySites.BlockedProperty.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
