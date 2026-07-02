public sealed class AllowedPropertySiteExample
{
	// ARCH001: allowedSites="Property" does not allow the constructor parameter above.
	public AllowedPropertySiteExample(AllowedPropertyType wrongSite) => _ = wrongSite;

	// The property site is allowed.
	public AllowedPropertyType Allowed { get; set; } = null!;
}
