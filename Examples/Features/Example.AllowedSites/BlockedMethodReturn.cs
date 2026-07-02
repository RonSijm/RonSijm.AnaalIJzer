public sealed class BlockedMethodReturnSiteExample
{
	// A constructor parameter is not blocked, so this constructor is allowed.
	public BlockedMethodReturnSiteExample(BlockedMethodReturnType allowed) => _ = allowed;

	// ARCH001: blockedSites="MethodReturn" blocks this return type.
	public BlockedMethodReturnType WrongSite() => null!;
}
