// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.New;

public sealed class AllowedNewSiteExample
{
    // ARCH001: allowedSites="New" does not allow the constructor parameter above.
    public AllowedNewSiteExample(AllowedNewType wrongSite)
    {
        _ = wrongSite;
    }

    // The new-expression site is allowed.
    public void Run()
    {
        _ = new AllowedNewType();
    }
}