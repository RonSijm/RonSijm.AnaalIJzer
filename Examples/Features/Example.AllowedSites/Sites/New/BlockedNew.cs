// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.New;

public sealed class BlockedNewSiteExample
{
    // A constructor parameter is not blocked, so this constructor is allowed.
    public BlockedNewSiteExample(BlockedNewType allowed)
    {
        _ = allowed;
    }

    // ARCH001: blockedSites="New" blocks this new-expression.
    public void Run()
    {
        _ = new BlockedNewType();
    }
}