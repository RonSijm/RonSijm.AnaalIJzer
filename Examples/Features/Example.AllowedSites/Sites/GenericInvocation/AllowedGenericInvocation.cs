// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.GenericInvocation;

public sealed class AllowedGenericInvocationSiteExample
{
    // ARCH001: allowedSites="GenericInvocation" does not allow the constructor parameter above.
    public AllowedGenericInvocationSiteExample(AllowedGenericInvocationType wrongSite)
    {
        _ = wrongSite;
    }

    // The generic invocation site is allowed.
    public void Run()
    {
        _ = Resolve<AllowedGenericInvocationType>();
    }

    private static T Resolve<T>() where T : class
    {
        return null!;
    }
}