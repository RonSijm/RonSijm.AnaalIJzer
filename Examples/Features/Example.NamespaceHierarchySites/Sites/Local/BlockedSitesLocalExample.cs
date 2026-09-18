// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedLocal
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedLocal.Requests
{
    public sealed class BlockedSitesLocalExample
    {
        // Local is excluded from this block by blockedSites.
        public void Prepare()
        {
            Restaurant.NamespaceHierarchySites.BlockedLocal.PantryIngredient ingredient = null!;
            _ = ingredient;
        }

        // ARCH_NS_007: a constructor remains blocked.
        public BlockedSitesLocalExample(Restaurant.NamespaceHierarchySites.BlockedLocal.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
