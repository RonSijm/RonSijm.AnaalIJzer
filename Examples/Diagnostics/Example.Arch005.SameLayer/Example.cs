// ARCH005: PizzaChef depends on ISauceChef, and both are in the Chef layer.
// Types within the same layer may not depend on each other
// even if no explicit edge is configured between them.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels strict="false">

  <Layer name="Chef">
    <Class endsWith="Chef" />
  </Layer>

  <Layer name="Pantry">
    <Class endsWith="Pantry" />
  </Layer>

  <AllowedDependency from="Chef" to="Pantry" />

</ArchitecturalLevels>
""")]

public interface IIngredientPantry { }

// Chef -> Pantry is allowed.
public class DessertChef(IIngredientPantry pantry) { }

// ARCH005: one chef should not directly command another chef.
public interface ISauceChef { }
public class PizzaChef(ISauceChef sauceChef) { }
