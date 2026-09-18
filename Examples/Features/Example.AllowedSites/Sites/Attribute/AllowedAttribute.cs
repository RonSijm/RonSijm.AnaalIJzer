// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Attribute;

[AllowedAttributeType]
public sealed class AllowedAttributeSiteExample
{
    // ARCH_DEP_001: allowedSites="Attribute" does not allow this constructor parameter.
    public AllowedAttributeSiteExample(AllowedAttributeType wrongSite)
    {
        _ = wrongSite;
    }
}