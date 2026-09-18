// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedStaticMember
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

namespace Restaurant.NamespaceHierarchySites.AllowedStaticMember.Requests
{
    public sealed class AllowedSitesStaticMemberExample
    {
        // Constructor is allowed because the block only selects StaticMember.
        public AllowedSitesStaticMemberExample(Restaurant.NamespaceHierarchySites.AllowedStaticMember.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        public void Prepare()
        {
            // ARCH_NS_007: allowedSites selects StaticMember as the site where this block applies.
            Restaurant.NamespaceHierarchySites.AllowedStaticMember.PantryUtility.Use();
        }
    }
}
