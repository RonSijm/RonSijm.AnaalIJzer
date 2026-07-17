// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.MethodReturn;

public sealed class BlockedMethodReturnSiteExample
{
    // A constructor parameter is not blocked, so this constructor is allowed.
    public BlockedMethodReturnSiteExample(BlockedMethodReturnType allowed)
    {
        _ = allowed;
    }

    // ARCH001: blockedSites="MethodReturn" blocks this return type.
    public BlockedMethodReturnType WrongSite()
    {
        return null!;
    }
}