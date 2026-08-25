// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="RepositoryQuerySurface">
    <Class endsWith="Queryable" />
    <VisibilityPolicy
      targets="Type"
      allowedAccessibilities="Internal, File"
      description="Repository query surfaces are implementation details." />
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch012.VisibilityPolicy;

// Valid: callers may use this fluent access point, but the type is not public API.
internal class LollyQueryable { }

// ARCH012: a repository-owned query surface must not become a public contract.
public class SourLollyQueryable { }
