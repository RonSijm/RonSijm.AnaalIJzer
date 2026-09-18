// ReSharper disable All - Justification: Example File
namespace Restaurant.NamespaceHierarchySites.AllowedInterfaceImplementation
{
    public interface IPantryRecipe
    {
    }
}

namespace Restaurant.NamespaceHierarchySites.AllowedInterfaceImplementation.Requests
{
    // ARCH_NS_007: allowedSites selects InterfaceImplementation as the site where this block applies.
    public sealed class AllowedSitesInterfaceImplementationExample : Restaurant.NamespaceHierarchySites.AllowedInterfaceImplementation.IPantryRecipe
    {
        // Constructor is allowed because the block only selects InterfaceImplementation.
        public AllowedSitesInterfaceImplementationExample(Restaurant.NamespaceHierarchySites.AllowedInterfaceImplementation.IPantryRecipe recipe)
        {
            _ = recipe;
        }
    }
}
