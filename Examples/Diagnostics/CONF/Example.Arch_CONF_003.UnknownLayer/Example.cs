// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="Application"><Class typeName="OrderService" /></Layer>
                                                    <AllowedDependency from="Application" to="TypoRepository" />
                                                  </ArchitecturalLevels>
                                                  """)]

// ARCH_CONF_003: TypoRepository is not a declared layer, so the edge cannot be evaluated.
namespace Example.Arch_CONF_003.UnknownLayer;

public sealed class OrderService;