// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="PersistenceEntities">
    <Namespace startsWith="Example.Arch_INH_001.InheritancePolicy.Persistence" />
    <InheritancePolicy
      typeKinds="Class"
      requiredBaseTypes="Entity"
      description="Persistence entities inherit the shared Entity base." />
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch_INH_001.InheritancePolicy.Framework
{
	public abstract class Entity { }
}

namespace Example.Arch_INH_001.InheritancePolicy.Persistence
{
	// Valid: persistence entities may inherit the shared Entity base.
	public class CandyEntity : Example.Arch_INH_001.InheritancePolicy.Framework.Entity { }

	// ARCH_INH_001: persistence entities must inherit Entity.
	public class SyrupEntity { }
}
