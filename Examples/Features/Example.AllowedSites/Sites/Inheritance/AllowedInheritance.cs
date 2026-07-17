// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Inheritance;

public sealed class AllowedInheritanceSiteExample : AllowedInheritanceType
{
    // ARCH001: allowedSites="Inheritance" does not allow this constructor parameter.
    public AllowedInheritanceSiteExample(AllowedInheritanceType wrongSite)
    {
        _ = wrongSite;
    }
}