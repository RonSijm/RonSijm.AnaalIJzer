public sealed class AllowedMethodReturnSiteExample
{
	// allowedSites="MethodReturn" allows this return type.
	public AllowedMethodReturnType Allowed() => null!;

	// ARCH001: the same dependency is not allowed at Site=Method.
	public void WrongSite(AllowedMethodReturnType wrongSite) => _ = wrongSite;
}
