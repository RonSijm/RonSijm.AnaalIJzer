// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Field;

public sealed class BlockedFieldSiteExample
{
    // A constructor parameter is not blocked, so this constructor is allowed.
    public BlockedFieldSiteExample(BlockedFieldType allowed)
    {
        _ = allowed;
    }

    // ARCH001: blockedSites="Field" blocks this field.
    private readonly BlockedFieldType _blocked = null!;
}