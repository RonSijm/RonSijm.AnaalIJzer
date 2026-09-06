// ReSharper disable All - Justification: Example File

#nullable disable

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <BehavioralOperations description="A prepared pizza announces itself exactly once to the dining room.">
      <MaximumOperationCount maximum="1" description="Ring the service bell at most once per pizza preparation.">
        <DeclarationMatcher>
          <Member endsWith="Pizza" memberKind="Method" />
        </DeclarationMatcher>
        <OperationMatcher kind="Invocation">
          <ContainingType typeName="ServiceBell" />
          <Member exactName="Ring" memberKind="Method" />
        </OperationMatcher>
      </MaximumOperationCount>
    </BehavioralOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch022.MaximumOperationCount;

public sealed class PizzaKitchen
{
	// Valid: the dining room receives one service bell notification.
	public void PrepareMargheritaPizza()
	{
		ServiceBell.Ring();
	}

	// ARCH022: the second ring exceeds the configured maximum.
	public void PrepareMysteryPizza()
	{
		ServiceBell.Ring();
		ServiceBell.Ring();
	}
}

public static class ServiceBell
{
	public static void Ring()
	{
	}
}
