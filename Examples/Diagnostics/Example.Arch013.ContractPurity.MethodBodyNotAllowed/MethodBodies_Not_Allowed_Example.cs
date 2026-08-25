// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Contracts">
    <Class endsWith="Contract" typeKind="Interface" />
    <ContractPolicy
      allowedTypeKinds="Interface"
      allowedMemberKinds="Method"
      allowMethodBodies="false"
      description="Contracts expose method signatures without bodies." />
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch013.ContractPurity.MethodBodyNotAllowed;

// Valid: abstract interface method.
public interface IOrderContract
{
	void Submit();
}

// ARCH013: the contract policy forbids method bodies in contract types.
public interface IKitchenContract
{
	void WarmPlate()
	{
	}
}
