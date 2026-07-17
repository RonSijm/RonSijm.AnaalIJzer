// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="LegacyKitchen">
                                                      <Class typeName="LegacyChef" />
                                                    </Layer>

                                                    <Layer name="AuditedKitchen" requireRecognizedDependencies="Constructor">
                                                      <Class typeName="AuditedChef" />
                                                    </Layer>
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.LayerScopedRecognizedDependencies;

public class MysteryBox { }

// Valid: legacy kitchen code may still have unclassified constructor ingredients.
public class LegacyChef(MysteryBox box) { }

// ARCH002: AuditedKitchen requires constructor dependencies to have a configured layer.
public class AuditedChef(MysteryBox box) { }