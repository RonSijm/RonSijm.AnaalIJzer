// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.BlockedInterfaceImplementation
{
    public interface IPantryRecipe
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.BlockedInterfaceImplementation.Requests
{
    // InterfaceImplementation is excluded from this block by blockedSites.
    public sealed class BlockedSitesInterfaceImplementationExample : Restaurant.NamespaceHierarchySites.BlockedInterfaceImplementation.IPantryRecipe
    {
        // ARCH_NS_007: every other site, including Constructor, remains blocked.
        public BlockedSitesInterfaceImplementationExample(Restaurant.NamespaceHierarchySites.BlockedInterfaceImplementation.IPantryRecipe recipe)
        {
            _ = recipe;
        }
    }
}
