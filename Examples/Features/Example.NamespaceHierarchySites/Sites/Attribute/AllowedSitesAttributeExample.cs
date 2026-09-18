// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedAttribute
{
    public sealed class PantryIngredient
    {
    }

    public sealed class PantryMarkerAttribute : System.Attribute
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedAttribute.Requests
{
    // ARCH_NS_007: allowedSites selects Attribute as the site where this block applies.
    [Restaurant.NamespaceHierarchySites.AllowedAttribute.PantryMarker]
    public sealed class AllowedSitesAttributeExample
    {
        // Constructor is allowed because the block only selects Attribute.
        public AllowedSitesAttributeExample(Restaurant.NamespaceHierarchySites.AllowedAttribute.PantryIngredient ingredient)
        {
            _ = ingredient;
        }
    }
}
