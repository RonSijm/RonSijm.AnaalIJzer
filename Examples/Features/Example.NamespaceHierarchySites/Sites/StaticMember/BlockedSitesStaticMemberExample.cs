// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedStaticMember
{
    public sealed class PantryIngredient
    {
    }

    public static class PantryUtility
    {
        public static void Use()
        {
        }
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedStaticMember.Requests
{
    public sealed class BlockedSitesStaticMemberExample
    {
        // StaticMember is excluded from this block by blockedSites.
        public void Prepare()
        {
            Restaurant.NamespaceHierarchySites.BlockedStaticMember.PantryUtility.Use();
        }

        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesStaticMemberExample(Restaurant.NamespaceHierarchySites.BlockedStaticMember.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
