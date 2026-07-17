// ReSharper disable All - Justification: Example File
using Example.AllowedSites.Shared;

namespace Example.AllowedSites.Sites.StaticMember;

public sealed class AllowedStaticMemberSiteExample
{
    // ARCH001: allowedSites="StaticMember" does not allow this constructor parameter.
    public AllowedStaticMemberSiteExample(AllowedStaticMemberType wrongSite)
    {
        _ = wrongSite;
    }

    public void Run()
    {
        AllowedStaticMemberType.Use();
    }
}