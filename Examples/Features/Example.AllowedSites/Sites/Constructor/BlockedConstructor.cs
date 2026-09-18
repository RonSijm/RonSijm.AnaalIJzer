// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Constructor;

public sealed class BlockedConstructorSiteExample
{
    // ARCH_DEP_001: blockedSites="Constructor" blocks the constructor parameter above.
    public BlockedConstructorSiteExample(BlockedConstructorType blocked)
    {
        _ = blocked;
    }

    // A method parameter is not blocked, so this is allowed.
    public void Allowed(BlockedConstructorType allowed)
    {
        _ = allowed;
    }
}