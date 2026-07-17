// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="Controller"><Class endsWith="Controller" /></Layer>
                                                    <Layer name="Application"><Class endsWith="Service" /></Layer>
                                                    <Layer name="Repository"><Class endsWith="Repository" /></Layer>
                                                    <AllowedDependency from="*" to="Repository" />
                                                    <BlockedDependency from="Controller" to="Repository" />
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.BlockedDependency;

// Allowed by the wildcard edge.
public sealed class OrderService(OrderRepository repository);

// ARCH001: the specific block overrides the wildcard allowance.
public sealed class OrderController(OrderRepository repository);

public sealed class OrderRepository;