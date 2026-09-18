// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedInheritance
{
    public class PantryRecipe
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedInheritance.Requests
{
    // ARCH_NS_007: allowedSites selects Inheritance as the site where this block applies.
    public sealed class AllowedSitesInheritanceExample : Restaurant.NamespaceHierarchySites.AllowedInheritance.PantryRecipe
    {
        // Constructor is allowed because the block only selects Inheritance.
        public AllowedSitesInheritanceExample(Restaurant.NamespaceHierarchySites.AllowedInheritance.PantryRecipe recipe)
        {
            _ = recipe;
        }
    }
}
