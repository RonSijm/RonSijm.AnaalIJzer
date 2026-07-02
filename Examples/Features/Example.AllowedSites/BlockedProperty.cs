public sealed class BlockedPropertySiteExample
{
	// A constructor parameter is not blocked, so this constructor is allowed.
	public BlockedPropertySiteExample(BlockedPropertyType allowed) => _ = allowed;

	// ARCH001: blockedSites="Property" blocks this property.
	public BlockedPropertyType Blocked { get; set; } = null!;
}
