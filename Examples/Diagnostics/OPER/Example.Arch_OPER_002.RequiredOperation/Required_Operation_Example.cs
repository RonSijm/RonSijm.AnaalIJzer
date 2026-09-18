// ReSharper disable All - Justification: Example File

#nullable disable

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <BehavioralOperations description="Every pizza preparation checks food safety before leaving the kitchen.">
      <RequiredOperation description="Preparing a pizza includes the configured safety check.">
        <DeclarationMatcher>
          <Member endsWith="Pizza" memberKind="Method" />
        </DeclarationMatcher>
        <OperationMatcher kind="Invocation">
          <ContainingType typeName="PizzaSafetyCheck" />
          <Member exactName="Validate" memberKind="Method" />
        </OperationMatcher>
      </RequiredOperation>
    </BehavioralOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch_OPER_002.RequiredOperation;

public sealed class PizzaKitchen
{
	// Valid: this preparation always performs the configured safety check.
	public void PrepareMargheritaPizza()
	{
		PizzaSafetyCheck.Validate();
		PizzaOven.Bake();
	}

	// ARCH_OPER_002: this pizza reaches the oven without the required safety check.
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
