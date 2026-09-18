// ReSharper disable All - Justification: Example File

using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Direct">
    <Class exactName="DirectTicketService" />
    <NameRules>
      <RequireMatchingNames valueTracking="Direct" description="Only inspect the immediate value at each movement site.">
        <Source endsWith="Id" />
        <Target endsWith="Id" />
      </RequireMatchingNames>
    </NameRules>
  </Layer>
  <Layer name="Tracked">
    <Class exactName="TrackedTicketService" />
    <NameRules>
      <RequireMatchingNames valueTracking="IntraProcedural" description="Follow unambiguous local aliases within this one method.">
        <Source endsWith="Id" />
        <Target endsWith="Id" />
      </RequireMatchingNames>
    </NameRules>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.NameRuleIntraProceduralTracking;

public sealed class DirectTicketService
{
    // Allowed in Direct mode: the immediate source is the neutral local name pending.
    public void ForwardCustomerId(int customerId)
    {
        var pending = customerId;
        Save(pending);
    }

    private static void Save(int orderId)
    {
    }
}

public sealed class TrackedTicketService
{
    // ARCH_NAME_008: IntraProcedural mode recovers customerId through the pending local.
    public void ForwardCustomerId(int customerId)
    {
        var pending = customerId;
        Save(pending);
    }

    // ARCH_NAME_008: the same bounded tracking applies to a return value.
	public int GetOrderId(int customerId)
	{
		var pending = customerId;

		return pending;
	}

	// ARCH_NAME_008: a lambda body can hide the same alias before it invokes Save.
	public void ForwardCustomerIdThroughLambda(int customerId)
	{
		Action save = () =>
		{
			var pending = customerId;
			Save(pending);
		};

		save();
	}

	private static void Save(int orderId)
	{
    }
}
