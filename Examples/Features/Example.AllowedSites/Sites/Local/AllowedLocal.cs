// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Local;

public sealed class AllowedLocalSiteExample
{
    // ARCH001: allowedSites="Local" does not allow the constructor parameter above.
    public AllowedLocalSiteExample(AllowedLocalType wrongSite) => _ = wrongSite;

    public void Run()
    {
        // The local site is allowed.
        AllowedLocalType allowed = null!;
        _ = allowed;
    }
}