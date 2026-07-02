public sealed class AllowedFieldSiteExample
{
	// ARCH001: allowedSites="Field" does not allow the constructor parameter above.
	public AllowedFieldSiteExample(AllowedFieldType wrongSite) => _ = wrongSite;

	// The field site is allowed.
	private readonly AllowedFieldType _allowed = null!;
}
