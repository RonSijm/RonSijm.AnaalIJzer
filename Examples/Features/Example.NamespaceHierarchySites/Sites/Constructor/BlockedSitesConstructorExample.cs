// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedConstructor
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedConstructor.Requests
{
    public sealed class BlockedSitesConstructorExample
    {
        // Constructor is excluded from this block by blockedSites.
        public BlockedSitesConstructorExample(Restaurant.NamespaceHierarchySites.BlockedConstructor.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // ARCH_NS_007: every other site, including Field, remains blocked.
        private readonly Restaurant.NamespaceHierarchySites.BlockedConstructor.PantryIngredient _ingredient = null!;

        public void Inspect()
        {
            _ = _ingredient;
        }
    }
}
