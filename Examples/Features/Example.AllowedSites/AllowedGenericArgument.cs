using System;

public sealed class AllowedGenericArgumentSiteExample
{
	// allowedSites="GenericArgument" allows the Lazy<T> type argument above.
	public AllowedGenericArgumentSiteExample(Lazy<AllowedGenericArgumentType> allowed) => _ = allowed;

	// ARCH001: the same dependency is not allowed at Site=Method.
	public void WrongSite(AllowedGenericArgumentType wrongSite) => _ = wrongSite;
}
