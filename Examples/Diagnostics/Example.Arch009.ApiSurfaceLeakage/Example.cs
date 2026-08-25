// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Service" />
    <ApiSurface description="Application APIs expose contracts, never repository-owned query surfaces.">
      <AllowedLayer path="/Contracts" />
      <BlockedLayer path="/RepositoryQuerySurface" />
    </ApiSurface>
  </Layer>
  <Layer name="Contracts">
    <Class endsWith="Projection" />
  </Layer>
  <Layer name="RepositoryQuerySurface">
    <Class endsWith="Queryable" />
  </Layer>
  <AllowedDependency from="Application" to="Contracts" />
  <AllowedDependency from="Application" to="RepositoryQuerySurface" />
</ArchitecturalLevels>
""")]

namespace Example.Arch009.ApiSurfaceLeakage;

public class LollyProjection { }

public class LollyQueryable { }

public class CandyOrderingService
{
	// Valid: projections are part of the public contract.
	public LollyProjection OrderProjectedLolly()
	{
		return null!;
	}

	// Valid: the service may use the repository query surface privately.
	private LollyQueryable BuildLollyQuery()
	{
		return null!;
	}

	// ARCH009: permission to use a type does not grant permission to expose it.
	public LollyQueryable OrderRawLolly()
	{
		return null!;
	}
}
