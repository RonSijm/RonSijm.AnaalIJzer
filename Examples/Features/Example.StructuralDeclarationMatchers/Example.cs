// ReSharper disable All - Justification: Example File

namespace Example.StructuralDeclarationMatchers;

public interface IPizzaProvider { }

public sealed class PizzaId { }

public sealed class DrinkId { }

public sealed class TenantId { }

// Valid: this request has the configured PizzaId + _tenantId shape and implements IPizzaProvider.
public sealed class GetPizzaRequest : IPizzaProvider
{
	private readonly TenantId _tenantId = new();

	public PizzaId PizzaId { get; } = new();
}

// Not matched by the rule: it ends with Request, but it is about drinks rather than pizza.
public sealed class GetDrinkRequest
{
	private readonly TenantId _tenantId = new();

	public DrinkId DrinkId { get; } = new();
}

// Not matched by the rule: it has PizzaId, but not the required tenant field.
public sealed class PublicPizzaRequest
{
	public PizzaId PizzaId { get; } = new();
}

// ARCH019: this request matches the full configured shape and therefore must implement IPizzaProvider.
public sealed class CreatePizzaRequest
{
	private readonly TenantId _tenantId = new();

	public PizzaId PizzaId { get; } = new();
}
