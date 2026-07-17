// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Attribute;

[BlockedAttributeType]
public sealed class BlockedAttributeSiteExample
{
    // The constructor is allowed, while the attribute above produces ARCH001.
    public BlockedAttributeSiteExample(BlockedAttributeType allowed)
    {
        _ = allowed;
    }
}