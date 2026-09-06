// ReSharper disable All - Justification: Example File

#nullable disable

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <BehavioralOperations description="A committed pizza order is no longer edited by the kitchen.">
      <ForbiddenOperationAfter description="Do not print a ticket after committing the order.">
        <DeclarationMatcher>
          <Member endsWith="Pizza" memberKind="Method" />
        </DeclarationMatcher>
        <OperationMatcher kind="Invocation">
          <ContainingType typeName="PizzaTicket" />
          <Member exactName="Print" memberKind="Method" />
        </OperationMatcher>
        <AfterOperation>
          <OperationMatcher kind="Invocation">
            <ContainingType typeName="PizzaOrder" />
            <Member exactName="Commit" memberKind="Method" />
          </OperationMatcher>
        </AfterOperation>
      </ForbiddenOperationAfter>
    </BehavioralOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch022.ForbiddenOperationAfter;

public sealed class PizzaKitchen
{
	// Valid: the ticket is prepared before the order is committed.
	public void PrepareMargheritaPizza()
	{
		PizzaTicket.Print();
		PizzaOrder.Commit();
	}

	// ARCH022: the kitchen prints another ticket after committing the order.
	public void PrepareMysteryPizza()
	{
		PizzaOrder.Commit();
		PizzaTicket.Print();
	}
}

public static class PizzaOrder
{
	public static void Commit()
	{
	}
}

public static class PizzaTicket
{
	public static void Print()
	{
	}
}
