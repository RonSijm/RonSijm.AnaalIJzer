// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="AssemblyCode">
                                                      <Assembly exactName="Example.AssemblyMatcher" />
                                                    </Layer>
                                                    <Layer name="Repository">
                                                      <Class typeName="OrderRepository" />
                                                    </Layer>
                                                    <AllowedDependency from="AssemblyCode" to="Repository" />
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.AssemblyMatcher;

// Allowed: the assembly-owned service may depend on its repository.
public sealed class OrderService(OrderRepository repository);

// ARCH004: the repository reverses the configured AssemblyCode -> Repository direction.
public sealed class OrderRepository(OrderService service);