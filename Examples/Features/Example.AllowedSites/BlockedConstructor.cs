public sealed class BlockedConstructorSiteExample
{
	// ARCH001: blockedSites="Constructor" blocks the constructor parameter above.
	public BlockedConstructorSiteExample(BlockedConstructorType blocked) => _ = blocked;

	// A method parameter is not blocked, so this is allowed.
	public void Allowed(BlockedConstructorType allowed) => _ = allowed;
}
