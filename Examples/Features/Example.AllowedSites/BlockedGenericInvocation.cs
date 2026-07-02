public sealed class BlockedGenericInvocationSiteExample
{
	// A constructor parameter is not blocked, so this constructor is allowed.
	public BlockedGenericInvocationSiteExample(BlockedGenericInvocationType allowed) => _ = allowed;

	// ARCH001: blockedSites="GenericInvocation" blocks this generic invocation.
	public void Run() => _ = Resolve<BlockedGenericInvocationType>();

	private static T Resolve<T>() where T : class => null!;
}
