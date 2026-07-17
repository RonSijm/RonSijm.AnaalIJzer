// ReSharper disable All - Justification: Example File
// ARCH001: TableWaiter depends on IIngredientPantry, but no Waiter -> Pantry
// relationship is configured. The waiter must ask a chef instead of entering
// the pantry.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>

                                                    <Layer name="Customer">
                                                      <Class endsWith="Customer" />
                                                    </Layer>

                                                    <Layer name="Waiter">
                                                      <Class endsWith="Waiter" />
                                                    </Layer>

                                                    <Layer name="Pantry">
                                                      <Class endsWith="Pantry" />
                                                    </Layer>

                                                    <Layer name="Chef">
                                                      <Class endsWith="Chef" />
                                                    </Layer>

                                                    <AllowedDependency from="Customer" to="Waiter" />
                                                    <AllowedDependency from="Waiter" to="Chef" />
                                                    </ArchitecturalLevels>
                                                  """)]

namespace Example.Arch001.NoEdge;

public interface IWaiter { }
public interface IIngredientPantry { }
public interface IChef { }

// Customer -> Waiter is allowed.
public class HungryCustomer(IWaiter waiter) { }

// Waiter -> Chef is allowed, but Waiter -> Pantry is not.
// The waiter should pass the order to the chef rather than enter the pantry.
public class TableWaiter(IChef chef, IIngredientPantry pantry) { }