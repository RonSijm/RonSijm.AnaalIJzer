// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.InterfaceImplementation;

public sealed class AllowedInterfaceImplementationSiteExample : IAllowedInterfaceImplementationType
{
    // ARCH001: allowedSites="InterfaceImplementation" does not allow this constructor parameter.
    public AllowedInterfaceImplementationSiteExample(IAllowedInterfaceImplementationType wrongSite)
    {
        _ = wrongSite;
    }
}