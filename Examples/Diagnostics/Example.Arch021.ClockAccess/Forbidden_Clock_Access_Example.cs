// ReSharper disable All - Justification: Example File

#nullable disable

using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Kitchen" />
    <ForbiddenOperations description="A kitchen asks the restaurant clock instead of checking the wall clock itself.">
      <ForbiddenOperation allowedSites="StaticMember" description="Direct system-clock reads hide a time dependency.">
        <OperationMatcher kind="PropertyRead" staticAccess="true">
          <ContainingType exactFullName="System.DateTime" />
          <Member exactName="UtcNow" memberKind="Property" />
        </OperationMatcher>
        <OperationMatcher kind="PropertyRead" staticAccess="true">
          <ContainingType exactFullName="System.DateTime" />
          <Member exactName="Now" memberKind="Property" />
        </OperationMatcher>
        <OperationMatcher kind="PropertyRead" staticAccess="true">
          <ContainingType exactFullName="System.DateTime" />
          <Member exactName="Today" memberKind="Property" />
        </OperationMatcher>
      </ForbiddenOperation>
    </ForbiddenOperations>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.Arch021.ClockAccess;

public sealed class PizzaClock
{
	public DateTime Now { get; } = new DateTime(2026, 9, 3, 18, 0, 0, DateTimeKind.Utc);
}

public sealed class PizzaKitchen(PizzaClock clock)
{
	// Valid: the kitchen gets time through its explicit restaurant-clock dependency.
	public DateTime PrepareWithClock()
	{
		return clock.Now;
	}

	// ARCH021: checking the wall clock directly hides the kitchen's time dependency.
	public DateTime PrepareAtUtcNow()
	{
		return DateTime.UtcNow;
	}

	// ARCH021: the local restaurant clock has the same architectural problem.
	public DateTime PrepareAtLocalNow()
	{
		return DateTime.Now;
	}

	// ARCH021: today's date is still a direct system-clock read.
	public DateTime PrepareToday()
	{
		return DateTime.Today;
	}
}
