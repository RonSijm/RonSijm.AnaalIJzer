// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.Method;

public sealed class AllowedMethodSiteExample
{
    // allowedSites="Method" allows this method parameter.
    public void Allowed(AllowedMethodType allowed)
    {
        _ = allowed;
    }

    // ARCH_DEP_001: the same dependency is not allowed at Site=MethodReturn.
    public AllowedMethodType WrongSite()
    {
        return null!;
    }
}