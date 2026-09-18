// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedNew
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedNew.Requests
{
    public sealed class AllowedSitesNewExample
    {
        // Constructor is allowed because the block only selects New.
        public AllowedSitesNewExample(Restaurant.NamespaceHierarchySites.AllowedNew.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        public void Prepare()
        {
            // ARCH_NS_007: allowedSites selects New as the site where this block applies.
            _ = new Restaurant.NamespaceHierarchySites.AllowedNew.PantryIngredient();
        }
    }
}
