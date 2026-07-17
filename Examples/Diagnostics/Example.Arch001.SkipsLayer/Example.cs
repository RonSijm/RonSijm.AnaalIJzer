// ReSharper disable All - Justification: Example File
// ARCH001: ImpatientCustomer skips the waiter and asks a chef directly.
// The configured relationship is Customer -> Waiter -> Chef.

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

                                                    <Layer name="Chef">
                                                      <Class endsWith="Chef" />
                                                    </Layer>

                                                    <AllowedDependency from="Customer" to="Waiter" />
                                                    <AllowedDependency from="Waiter" to="Chef" />
                                                    </ArchitecturalLevels>
                                                  """)]

namespace Example.Arch001.SkipsLayer;

public interface IWaiter { }
public interface IChef { }

// Customer -> Waiter is allowed.
public class HungryCustomer(IWaiter waiter) { }

// ARCH001: Customer -> Chef skips the required waiter.
public class ImpatientCustomer(IChef chef) { }