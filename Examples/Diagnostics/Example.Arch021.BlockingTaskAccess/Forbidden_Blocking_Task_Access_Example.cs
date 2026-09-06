// ReSharper disable All - Justification: Example File

#nullable disable

using System.Threading.Tasks;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Kitchen" />
    <ForbiddenOperations description="The kitchen awaits its preparation instead of blocking the dining room.">
      <ForbiddenOperation allowedSites="Method" description="Waiting synchronously ties up the kitchen counter.">
        <OperationMatcher kind="Invocation" staticAccess="false">
          <ContainingType typeName="Task" />
          <Member exactName="Wait" memberKind="Method" />
        </OperationMatcher>
      </ForbiddenOperation>
      <ForbiddenOperation allowedSites="Local" description="Reading Result turns a task into a blocking local dependency.">
        <OperationMatcher kind="PropertyRead" staticAccess="false">
          <ContainingType typeName="Task" />
          <Member exactName="Result" memberKind="Property" />
        </OperationMatcher>
      </ForbiddenOperation>
    </ForbiddenOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch021.BlockingTaskAccess;

public sealed class PizzaKitchen
{
	// Valid: waiting asynchronously leaves the kitchen free to keep working.
	public async Task<int> PrepareAsync(Task<int> preparation)
	{
		return await preparation;
	}

	// ARCH021: the kitchen counter should not stop for a task.
	public void PrepareByWaiting(Task preparation)
	{
		preparation.Wait();
	}

	// ARCH021: putting Result in a local still blocks for the pizza.
	public int PrepareFromResult(Task<int> preparation)
	{
		var pizzaNumber = preparation.Result;

		return pizzaNumber;
	}
}
