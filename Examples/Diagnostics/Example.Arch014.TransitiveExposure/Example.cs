// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Service" />
    <ApiSurface description="Application APIs expose contracts, including their public object graphs.">
      <TransitiveExposure maxDepth="3" description="Follow public contract members far enough to find hidden repository surfaces." />
      <AllowedLayer path="/Contracts" />
      <BlockedLayer path="/RepositoryQuerySurface" />
    </ApiSurface>
  </Layer>
  <Layer name="Contracts">
    <Class endsWith="Receipt" />
  </Layer>
  <Layer name="RepositoryQuerySurface">
    <Class endsWith="Queryable" />
  </Layer>
  <AllowedDependency from="Application" to="Contracts" />
  <AllowedDependency from="Contracts" to="RepositoryQuerySurface" />
</ArchitecturalLevels>
""")]

namespace Example.Arch014.TransitiveExposure;

public class ProjectedCandyReceipt
{
	public string LollyName { get; init; } = string.Empty;
}

public class CandyReceipt
{
	// This repository-owned access point makes the otherwise allowed receipt unsafe to expose.
	public LollyQueryable RawQuery { get; init; } = new();
}

public class LollyQueryable { }

public class CandyOrderingService
{
	// Valid: the complete public object graph contains projected values only.
	public ProjectedCandyReceipt OrderProjectedLolly()
	{
		var result = new ProjectedCandyReceipt();

		return result;
	}

	// ARCH014: CandyOrderingService.OrderRawLolly -> CandyReceipt.RawQuery -> LollyQueryable.
	public CandyReceipt OrderRawLolly()
	{
		var result = new CandyReceipt();

		return result;
	}
}
