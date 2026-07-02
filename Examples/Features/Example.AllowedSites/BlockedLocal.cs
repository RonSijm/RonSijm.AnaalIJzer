public sealed class BlockedLocalSiteExample
{
	// A constructor parameter is not blocked, so this constructor is allowed.
	public BlockedLocalSiteExample(BlockedLocalType allowed) => _ = allowed;

	public void Run()
	{
		// ARCH001: blockedSites="Local" blocks this local variable.
		BlockedLocalType blocked = null!;
		_ = blocked;
	}
}
