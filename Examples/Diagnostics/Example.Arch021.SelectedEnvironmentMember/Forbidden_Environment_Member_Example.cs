// ReSharper disable All - Justification: Example File

#nullable disable

using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Kitchen" />
    <ForbiddenOperations description="Kitchen behavior must not vary with the machine that happens to host it.">
      <ForbiddenOperation allowedSites="StaticMember" description="The kitchen cannot use the host machine name as a hidden ingredient.">
        <OperationMatcher kind="PropertyRead" staticAccess="true">
          <ContainingType exactFullName="System.Environment" />
          <Member exactName="MachineName" memberKind="Property" />
        </OperationMatcher>
      </ForbiddenOperation>
    </ForbiddenOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch021.SelectedEnvironmentMember;

public sealed class PizzaKitchen
{
	// Valid: a line separator does not make the recipe host-specific.
	public string PrintMenuLine()
	{
		return Environment.NewLine;
	}

	// ARCH021: machine identity is an infrastructure concern, not a kitchen ingredient.
	public string NameTheHost()
	{
		return Environment.MachineName;
	}
}
