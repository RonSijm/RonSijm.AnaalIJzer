// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedMethod
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedMethod.Requests
{
    public sealed class BlockedSitesMethodExample
    {
        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesMethodExample(Restaurant.NamespaceHierarchySites.BlockedMethod.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // Method is excluded from this block by blockedSites.
        public void Receive(Restaurant.NamespaceHierarchySites.BlockedMethod.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
