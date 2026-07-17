// ReSharper disable All - Justification: Example File
// ARCH002: ExperimentalChef depends on MysteryBox,
// which is not assigned to any configured layer. With requireRecognizedDependencies="Constructor" the
// analyzer reports every unrecognized constructor dependency as a violation.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels requireRecognizedDependencies="Constructor">

                                                    <Layer name="Chef">
                                                      <Class endsWith="Chef" />
                                                    </Layer>

                                                    <Layer name="Pantry">
                                                      <Class endsWith="Pantry" />
                                                    </Layer>

                                                    <AllowedDependency from="Chef" to="Pantry" />
                                                    </ArchitecturalLevels>
                                                  """)]

namespace Example.Arch002.UnrecognizedDependency;

public interface IIngredientPantry { }

// Chef -> Pantry is allowed.
public class PizzaChef(IIngredientPantry pantry) { }

// ARCH002: MysteryBox is not listed in any configured layer.
public sealed class MysteryBox { }
public class ExperimentalChef(MysteryBox box) { }