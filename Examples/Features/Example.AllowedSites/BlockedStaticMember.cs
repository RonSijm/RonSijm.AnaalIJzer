public sealed class BlockedStaticMemberSiteExample
{
	// A constructor parameter is not blocked, so this constructor is allowed.
	public BlockedStaticMemberSiteExample(BlockedStaticMemberType allowed) => _ = allowed;

	// ARCH001: blockedSites="StaticMember" blocks this static call.
	public void Run() => BlockedStaticMemberType.Use();
}
