// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedGenericArgument
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedGenericArgument.Requests
{
    public sealed class BlockedSitesGenericArgumentExample
    {
        // GenericArgument is excluded from this block by blockedSites.
        private readonly System.Lazy<Restaurant.NamespaceHierarchySites.BlockedGenericArgument.PantryIngredient>? _ingredients = null;

        public void Inspect()
        {
            _ = _ingredients;
        }

        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesGenericArgumentExample(Restaurant.NamespaceHierarchySites.BlockedGenericArgument.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
