using System;

public sealed class BlockedGenericArgumentSiteExample
{
	// A constructor parameter is not blocked, so the first parameter above is allowed.
	// ARCH001: blockedSites="GenericArgument" blocks the Lazy<T> type argument above.
	public BlockedGenericArgumentSiteExample(BlockedGenericArgumentType allowed, Lazy<BlockedGenericArgumentType> blocked)
	{
		_ = allowed;
		_ = blocked;
	}
}
