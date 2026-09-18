// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedField
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedField.Requests
{
    public sealed class AllowedSitesFieldExample
    {
        // Constructor is allowed because the block only selects Field.
        public AllowedSitesFieldExample(Restaurant.NamespaceHierarchySites.AllowedField.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // ARCH_NS_007: allowedSites selects Field as the site where this block applies.
        private readonly Restaurant.NamespaceHierarchySites.AllowedField.PantryIngredient _ingredient = null!;

        public void Inspect()
        {
            _ = _ingredient;
        }
    }
}
