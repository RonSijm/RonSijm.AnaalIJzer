// ReSharper disable All - Justification: Example File
// Each class below intentionally triggers one of the five ARCH00X diagnostics.
// The committed report at Examples/Documentation/Generated/architectural-violations.md is the
// rendered Markdown output of exactly these violations.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels requireRecognizedDependencies="Constructor"
                                                                       enableReport="true"
                                                                       reportPath="../Generated/architectural-violations.md"
                                                                       enableDocumentation="true"
                                                                       documentationPath="../Generated/architecture-documentation.md">

                                                    <Forbidden>
                                                      <Class endsWith="Store" comment="Persistence types must use the Repository suffix.">
                                                        <Fix Rename="Repository" />
                                                      </Class>
                                                    </Forbidden>

                                                    <Layer name="Presentation">
                                                      <Class endsWith="Endpoint" />
                                                    </Layer>

                                                    <Layer name="Application">
                                                      <Class endsWith="Service" />
                                                      <Class endsWith="Manager" />
                                                      <Class endsWith="Coordinator" />
                                                    </Layer>

                                                    <Layer name="Persistence">
                                                      <Class endsWith="Repository" />
                                                    </Layer>

                                                    <AllowedDependency from="Presentation" to="Application" />
                                                    <AllowedDependency from="Application" to="Persistence" />

                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.ReportDemo;

public interface IOrderService { }
public interface ISecondaryOrderService { }
public interface IOrderRepository { }
public sealed class MysteryDependency { }

// ARCH001: Presentation -> Persistence has no configured edge.
public class AdminEndpoint(IOrderRepository repository) { }

// ARCH004: Persistence -> Application reverses the configured edge.
public class OrderRepository(IOrderService service) { }

// ARCH005: OrderService and ISecondaryOrderService are both in Application.
public class OrderService(ISecondaryOrderService secondaryService) { }

// ARCH003: OrderStore matches the forbidden Store pattern.
public class OrderStore { }
public class OrderManager(OrderStore store) { }

// ARCH002: MysteryDependency is unrecognized while recognized dependencies are required.
public class OrderCoordinator(MysteryDependency dependency) { }