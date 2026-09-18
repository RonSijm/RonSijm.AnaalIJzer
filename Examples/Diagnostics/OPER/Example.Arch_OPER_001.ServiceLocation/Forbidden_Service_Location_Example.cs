// ReSharper disable All - Justification: Example File

#nullable disable

using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Kitchen" />
    <ForbiddenOperations description="The kitchen receives ingredients explicitly; it does not search the restaurant's hidden menu.">
      <ForbiddenOperation allowedSites="MethodReturn" description="A service-located result is still a hidden dependency.">
        <OperationMatcher kind="Invocation" staticAccess="false">
          <ContainingType exactFullName="System.IServiceProvider" />
          <Member exactName="GetService" memberKind="Method" />
        </OperationMatcher>
      </ForbiddenOperation>
    </ForbiddenOperations>
  </Layer>
  <Layer name="Composition">
    <Class endsWith="CompositionRoot" />
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch_OPER_001.ServiceLocation;

public sealed class PizzaKitchen
{
	// ARCH_OPER_001: the kitchen found its dependency through a hidden menu.
	public object Prepare(IServiceProvider services) => services.GetService(typeof(PizzaKitchen));
}

public sealed class PizzaCompositionRoot
{
	// Valid: assembling the restaurant is the composition root's own job.
	public object Compose(IServiceProvider services) => services.GetService(typeof(PizzaKitchen));
}
