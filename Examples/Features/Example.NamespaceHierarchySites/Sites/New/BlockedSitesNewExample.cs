// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedNew
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedNew.Requests
{
    public sealed class BlockedSitesNewExample
    {
        // New is excluded from this block by blockedSites.
        public void Prepare()
        {
            _ = new Restaurant.NamespaceHierarchySites.BlockedNew.PantryIngredient();
        }

        // ARCH_NS_007: a constructor remains blocked.
        public BlockedSitesNewExample(Restaurant.NamespaceHierarchySites.BlockedNew.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
