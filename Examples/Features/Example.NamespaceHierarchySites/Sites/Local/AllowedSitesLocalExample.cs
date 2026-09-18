// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedLocal
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedLocal.Requests
{
    public sealed class AllowedSitesLocalExample
    {
        // Constructor is allowed because the block only selects Local.
        public AllowedSitesLocalExample(Restaurant.NamespaceHierarchySites.AllowedLocal.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        public void Prepare()
        {
            // ARCH_NS_007: allowedSites selects Local as the site where this block applies.
            Restaurant.NamespaceHierarchySites.AllowedLocal.PantryIngredient ingredient = null!;
            _ = ingredient;
        }
    }
}
