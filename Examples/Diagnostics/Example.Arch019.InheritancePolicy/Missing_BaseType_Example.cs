// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="PersistenceEntities">
    <Namespace startsWith="Example.Arch019.InheritancePolicy.Persistence" />
    <InheritancePolicy
      typeKinds="Class"
      requiredBaseTypes="Entity"
      description="Persistence entities inherit the shared Entity base." />
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch019.InheritancePolicy.Framework
{
	public abstract class Entity { }
}

namespace Example.Arch019.InheritancePolicy.Persistence
{
	// Valid: persistence entities may inherit the shared Entity base.
	public class CandyEntity : Example.Arch019.InheritancePolicy.Framework.Entity { }

	// ARCH019: persistence entities must inherit Entity.
	public class SyrupEntity { }
}
