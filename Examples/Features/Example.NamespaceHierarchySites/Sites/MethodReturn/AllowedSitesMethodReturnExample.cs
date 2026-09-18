// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedMethodReturn
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedMethodReturn.Requests
{
    public sealed class AllowedSitesMethodReturnExample
    {
        // Constructor is allowed because the block only selects MethodReturn.
        public AllowedSitesMethodReturnExample(Restaurant.NamespaceHierarchySites.AllowedMethodReturn.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // ARCH_NS_007: allowedSites selects MethodReturn as the site where this block applies.
        public Restaurant.NamespaceHierarchySites.AllowedMethodReturn.PantryIngredient Receive()
        {
            return null!;
        }
    }
}
