// ReSharper disable All - Justification: Example File
using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
<Layer name="FallbackServices">
<Class endsWith="Service">
<Method exactName="PizzaDelivery">
<Throw />
</Method>
</Class>
<InheritancePolicy typeKinds="Class" requiredInterfaces="IPizzaFallback" />
</Layer>
<Layer name="GuardedCatalogs">
<Class endsWith="Catalog">
<Property exactName="PizzaId">
<Throw typeName="InvalidOperationException" />
</Property>
</Class>
<InheritancePolicy typeKinds="Class" requiredInterfaces="IPizzaCatalogGuard" />
</Layer>
</ArchitecturalLevels>
""")]

namespace Example.DeclarationObservationMatchers;

public interface IPizzaFallback { }

public interface IPizzaCatalogGuard { }

public sealed class PizzaId { }

// Valid: this delivery advertises that callers should expect fallback behavior.
public sealed class RecoveringPizzaDeliveryService : IPizzaFallback
{
	public void PizzaDelivery()
	{
		throw new InvalidOperationException("Oven offline.");
	}
}

// ARCH019: the method body throws, so the service must implement IPizzaFallback.
public sealed class CrashingPizzaDeliveryService
{
	public void PizzaDelivery()
	{
		throw new InvalidOperationException("Courier vanished into the night.");
	}
}

// Valid: the dangerous property is explicit about the guard contract.
public sealed class GuardedPizzaCatalog : IPizzaCatalogGuard
{
	public PizzaId PizzaId => throw new InvalidOperationException("Catalog is warming up.");
}

// ARCH019: the throwing PizzaId property must implement IPizzaCatalogGuard.
public sealed class ExplosivePizzaCatalog
{
	public PizzaId PizzaId => throw new InvalidOperationException("Catalog unavailable.");
}
