// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedMethodReturn
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedMethodReturn.Requests
{
    public sealed class BlockedSitesMethodReturnExample
    {
        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesMethodReturnExample(Restaurant.NamespaceHierarchySites.BlockedMethodReturn.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // MethodReturn is excluded from this block by blockedSites.
        public Restaurant.NamespaceHierarchySites.BlockedMethodReturn.PantryIngredient Receive()
        {
            return null!;
        }
    }
}
