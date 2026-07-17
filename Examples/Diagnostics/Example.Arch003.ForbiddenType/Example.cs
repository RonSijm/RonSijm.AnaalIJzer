// ReSharper disable All - Justification: Example File
// ARCH003: ReportingService depends on LegacyOrderStore, which matches the
// forbidden pattern endsWith="Store". The analyzer reports ARCH003 and —
// because <Fix Rename="Repository"> is configured — offers a one-click
// rename to LegacyOrderRepository in Visual Studio.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>

                                                    <Forbidden>
                                                      <Class endsWith="Store" comment="Persistence types must use the Repository suffix.">
                                                        <Fix Rename="Repository" />
                                                      </Class>
                                                    </Forbidden>

                                                    <Layer name="Application">
                                                      <Class endsWith="Service" />
                                                    </Layer>

                                                    <Layer name="Persistence">
                                                      <Class endsWith="Repository" />
                                                    </Layer>

                                                    <AllowedDependency from="Application" to="Persistence" />

                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.Arch003.ForbiddenType;

public class OrderRepository { }

// Application -> Persistence is allowed.
public class OrderService(OrderRepository repository) { }

// ARCH003: LegacyOrderStore matches the forbidden suffix.
public class LegacyOrderStore { }
public class ReportingService(LegacyOrderStore store) { }