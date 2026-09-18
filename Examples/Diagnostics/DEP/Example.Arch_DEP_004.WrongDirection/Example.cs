// ReSharper disable All - Justification: Example File
// ARCH_DEP_004: IngredientPantry depends on IChef. The configured direction is
// Chef -> Pantry, so Pantry -> Chef is the reverse relationship.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>

                                                    <Layer name="Chef">
                                                      <Class endsWith="Chef" />
                                                    </Layer>

                                                    <Layer name="Pantry">
                                                      <Class endsWith="Pantry" />
                                                    </Layer>

                                                    <AllowedDependency from="Chef" to="Pantry" />
                                                    </ArchitecturalLevels>
                                                  """)]

namespace Example.Arch_DEP_004.WrongDirection;

public interface IIngredientPantry { }
public interface IChef { }

// Chef -> Pantry is allowed.
public class PizzaChef(IIngredientPantry pantry) { }

// ARCH_DEP_004: Pantry -> Chef reverses the configured relationship.
// The pantry supplies the chef; it does not direct the chef.
public class IngredientPantry(IChef chef) { }