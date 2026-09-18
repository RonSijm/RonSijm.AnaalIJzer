// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedMethod
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedMethod.Requests
{
    public sealed class AllowedSitesMethodExample
    {
        // Constructor is allowed because the block only selects Method.
        public AllowedSitesMethodExample(Restaurant.NamespaceHierarchySites.AllowedMethod.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // ARCH_NS_007: allowedSites selects Method as the site where this block applies.
        public void Receive(Restaurant.NamespaceHierarchySites.AllowedMethod.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
