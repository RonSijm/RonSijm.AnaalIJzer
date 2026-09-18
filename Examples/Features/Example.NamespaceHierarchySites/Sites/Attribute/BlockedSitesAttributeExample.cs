// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedAttribute
{
    public sealed class PantryIngredient
    {
    }

    public sealed class PantryMarkerAttribute : System.Attribute
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedAttribute.Requests
{
    // Attribute is excluded from this block by blockedSites.
    [Restaurant.NamespaceHierarchySites.BlockedAttribute.PantryMarker]
    public sealed class BlockedSitesAttributeExample
    {
        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesAttributeExample(Restaurant.NamespaceHierarchySites.BlockedAttribute.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
