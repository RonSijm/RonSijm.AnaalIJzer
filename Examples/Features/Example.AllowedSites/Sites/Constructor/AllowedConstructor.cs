// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Constructor;

public sealed class AllowedConstructorSiteExample
{
    // allowedSites="Constructor" allows the constructor parameter above.
    public AllowedConstructorSiteExample(AllowedConstructorType allowed) => _ = allowed;

    // ARCH001: the same dependency is not allowed at Site=Field.
    private readonly AllowedConstructorType _wrongSite = null!;
}