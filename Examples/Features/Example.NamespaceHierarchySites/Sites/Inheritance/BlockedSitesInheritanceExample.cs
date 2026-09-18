// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedInheritance
{
    public class PantryRecipe
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedInheritance.Requests
{
    // Inheritance is excluded from this block by blockedSites.
    public sealed class BlockedSitesInheritanceExample : Restaurant.NamespaceHierarchySites.BlockedInheritance.PantryRecipe
    {
        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesInheritanceExample(Restaurant.NamespaceHierarchySites.BlockedInheritance.PantryRecipe recipe)
        {
            _ = recipe;
        }
    }
}
