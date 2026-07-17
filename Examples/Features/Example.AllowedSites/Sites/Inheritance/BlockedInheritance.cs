// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Inheritance;

public sealed class BlockedInheritanceSiteExample : BlockedInheritanceType
{
    // The constructor is allowed, while the inheritance above produces ARCH001.
    public BlockedInheritanceSiteExample(BlockedInheritanceType allowed)
    {
        _ = allowed;
    }
}