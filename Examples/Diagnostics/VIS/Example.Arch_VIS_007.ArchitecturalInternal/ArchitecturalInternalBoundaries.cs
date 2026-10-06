// ReSharper disable All - Justification: Example File

using System.Reflection;
using RonSijm.AnaalIJzer.Annotations;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Persistence">
    <Class typeName="EntityProjection" />
    <Class typeName="ProjectionRepository" />
  </Layer>
  <Layer name="Application">
    <Class typeName="OrderingService" />
  </Layer>
  <AllowedDependency from="Application" to="Persistence" />
  <AllowedDependency from="Persistence" to="Persistence" />
</ArchitecturalLevels>
""")]

namespace Example.Arch_VIS_007.ArchitecturalInternal;

[ClassInternal]
public sealed class ReflectionProjection
{
    public static ReflectionProjection Create() => new();
}

internal sealed class ReflectionProjectionConsumer
{
    // ARCH_VIS_007: public for reflection does not mean another class may use it directly.
    private void Receive(ReflectionProjection projection) { }
}

[AssemblyInternal]
public sealed class AssemblyProjection { }

public sealed class ProjectionFacade
{
    // ARCH_VIS_007: an assembly-internal type must not leak through a public signature.
    public AssemblyProjection Leak() => new();
}

[LayerInternal]
public sealed class EntityProjection { }

internal sealed class ProjectionRepository
{
    // Valid: the repository and projection belong to the same Persistence layer.
    private EntityProjection Build() => new();
}

internal sealed class OrderingService
{
    // ARCH_VIS_007: Application may depend on Persistence, but not on its layer-internal projection.
    private void Receive(EntityProjection projection) { }
}
