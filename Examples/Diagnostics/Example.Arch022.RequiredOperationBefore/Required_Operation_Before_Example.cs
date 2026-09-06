// ReSharper disable All - Justification: Example File

#nullable disable

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <BehavioralOperations description="A chef checks a pizza before baking it.">
      <RequiredOperationBefore description="Validation happens before the oven mutates the pizza.">
        <DeclarationMatcher>
          <Member endsWith="Pizza" memberKind="Method" />
        </DeclarationMatcher>
        <OperationMatcher kind="Invocation">
          <ContainingType typeName="PizzaSafetyCheck" />
          <Member exactName="Validate" memberKind="Method" />
        </OperationMatcher>
        <BeforeOperation>
          <OperationMatcher kind="Invocation">
            <ContainingType typeName="PizzaOven" />
            <Member exactName="Bake" memberKind="Method" />
          </OperationMatcher>
        </BeforeOperation>
      </RequiredOperationBefore>
    </BehavioralOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch022.RequiredOperationBefore;

public sealed class PizzaKitchen
{
	// Valid: the safety check dominates the baking operation.
	public void PrepareMargheritaPizza()
	{
		PizzaSafetyCheck.Validate();
		PizzaOven.Bake();
	}

	// ARCH022: baking occurs without first performing the configured safety check.
	public void PrepareMysteryPizza()
	{
		PizzaOven.Bake();
	}
}

public static class PizzaSafetyCheck
{
	public static void Validate()
	{
	}
}

public static class PizzaOven
{
	public static void Bake()
	{
	}
}
