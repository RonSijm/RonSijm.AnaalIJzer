// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedGenericArgument
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedGenericArgument.Requests
{
    public sealed class AllowedSitesGenericArgumentExample
    {
        // Constructor is allowed because the block only selects GenericArgument.
        public AllowedSitesGenericArgumentExample(Restaurant.NamespaceHierarchySites.AllowedGenericArgument.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // ARCH_NS_007: allowedSites selects GenericArgument as the site where this block applies.
        private readonly System.Lazy<Restaurant.NamespaceHierarchySites.AllowedGenericArgument.PantryIngredient>? _ingredients = null;

        public void Inspect()
        {
            _ = _ingredients;
        }
    }
}
