public sealed class AllowedMethodSiteExample
{
	// allowedSites="Method" allows this method parameter.
	public void Allowed(AllowedMethodType allowed) => _ = allowed;

	// ARCH001: the same dependency is not allowed at Site=MethodReturn.
	public AllowedMethodType WrongSite() => null!;
}
