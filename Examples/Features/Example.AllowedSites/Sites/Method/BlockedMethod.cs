// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Method;

public sealed class BlockedMethodSiteExample
{
    // A constructor parameter is not blocked, so this constructor is allowed.
    public BlockedMethodSiteExample(BlockedMethodType allowed)
    {
        _ = allowed;
    }

    // ARCH001: blockedSites="Method" blocks this method parameter.
    public void WrongSite(BlockedMethodType blocked)
    {
        _ = blocked;
    }
}