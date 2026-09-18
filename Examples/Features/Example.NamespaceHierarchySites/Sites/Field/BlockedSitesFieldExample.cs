// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedField
{
    public sealed class PantryIngredient
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedField.Requests
{
    public sealed class BlockedSitesFieldExample
    {
        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesFieldExample(Restaurant.NamespaceHierarchySites.BlockedField.PantryIngredient ingredient)
        {
            _ = ingredient;
        }

        // Field is excluded from the block, so this field is allowed.
        private readonly Restaurant.NamespaceHierarchySites.BlockedField.PantryIngredient _ingredient = null!;

        public void Inspect()
        {
            _ = _ingredient;
        }
    }
}
