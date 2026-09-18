// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedGenericInvocation
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedGenericInvocation.Requests
{
    public sealed class AllowedSitesGenericInvocationExample
    {
        // Constructor is allowed because the block only selects GenericInvocation.
        public AllowedSitesGenericInvocationExample(Restaurant.NamespaceHierarchySites.AllowedGenericInvocation.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        public void Prepare()
        {
            // ARCH_NS_007: allowedSites selects GenericInvocation as the site where this block applies.
            _ = Resolve<Restaurant.NamespaceHierarchySites.AllowedGenericInvocation.PantryIngredient>();
        }

        private static T Resolve<T>()
        {
            var result = default(T)!;

            return result;
        }
    }
}
