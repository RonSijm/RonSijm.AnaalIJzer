// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Contracts">
    <Class endsWith="Contract" typeKind="Interface" />
    <ContractPolicy
      allowedTypeKinds="Interface"
      allowedMemberKinds="Property"
      allowedPropertyAccessors="Get"
      description="Contracts expose getter-only properties." />
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch013.ContractPurity;

// Valid: getter-only contract property.
public interface IOrderContract
{
	string Name { get; }
}

// ARCH013: the contract policy allows only a getter, not a setter.
public interface IPizzaContract
{
	string Name { get; set; }
}
