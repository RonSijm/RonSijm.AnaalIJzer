// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.InterfaceImplementation;

public sealed class BlockedInterfaceImplementationSiteExample : IBlockedInterfaceImplementationType
{
    // The constructor is allowed, while the interface implementation above produces ARCH001.
    public BlockedInterfaceImplementationSiteExample(IBlockedInterfaceImplementationType allowed)
    {
        _ = allowed;
    }
}