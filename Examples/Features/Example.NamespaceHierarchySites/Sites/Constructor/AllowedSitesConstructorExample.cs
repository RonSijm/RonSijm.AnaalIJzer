// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedConstructor
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedConstructor.Requests
{
    public sealed class AllowedSitesConstructorExample
    {
        // ARCH_NS_007: allowedSites selects Constructor as the site where this block applies.
        public AllowedSitesConstructorExample(Restaurant.NamespaceHierarchySites.AllowedConstructor.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // The same parent type is allowed at Method because the block only selects Constructor.
        public void Receive(Restaurant.NamespaceHierarchySites.AllowedConstructor.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
