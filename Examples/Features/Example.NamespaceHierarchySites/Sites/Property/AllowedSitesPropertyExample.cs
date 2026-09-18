// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedProperty
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedProperty.Requests
{
    public sealed class AllowedSitesPropertyExample
    {
        // Constructor is allowed because the block only selects Property.
        public AllowedSitesPropertyExample(Restaurant.NamespaceHierarchySites.AllowedProperty.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // ARCH_NS_007: allowedSites selects Property as the site where this block applies.
        public Restaurant.NamespaceHierarchySites.AllowedProperty.PantryIngredient Ingredient { get; } = null!;
    }
}
