// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedGenericInvocation
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedGenericInvocation.Requests
{
    public sealed class BlockedSitesGenericInvocationExample
    {
        // GenericInvocation is excluded from this block by blockedSites.
        public void Prepare()
        {
            _ = Resolve<Restaurant.NamespaceHierarchySites.BlockedGenericInvocation.PantryIngredient>();
        }

        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesGenericInvocationExample(Restaurant.NamespaceHierarchySites.BlockedGenericInvocation.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        private static T Resolve<T>()
        {
            var result = default(T)!;

            return result;
        }
    }
}
