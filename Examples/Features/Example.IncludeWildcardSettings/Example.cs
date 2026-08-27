// ReSharper disable All - Justification: Example File
// ARCH001: CuriousWaiter skips the Chef layer and grabs ingredients directly.
// The layer rules and allowed flow come from drop-in .anl files matched by
// <Include path="*.anl" />.

namespace Example.IncludeWildcardSettings;

public interface IPizzaChef { }
public interface IIngredientPantry { }

// Waiter -> Chef is allowed by a drop-in rule file.
public class TableWaiter(IPizzaChef chef) { }

// Chef -> Pantry is allowed by a drop-in rule file.
public class PizzaChef(IIngredientPantry pantry) { }

// ARCH001: Waiter -> Pantry is not part of the allowed restaurant flow.
public class CuriousWaiter(IIngredientPantry pantry) { }
