// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.MethodReturn;

public sealed class AllowedMethodReturnSiteExample
{
    // allowedSites="MethodReturn" allows this return type.
    public AllowedMethodReturnType Allowed()
    {
        return null!;
    }

    // ARCH001: the same dependency is not allowed at Site=Method.
    public void WrongSite(AllowedMethodReturnType wrongSite)
    {
        _ = wrongSite;
    }
}