// ReSharper disable All - Justification: Example File
using System.Reflection;

// Inline XML keeps tiny examples readable: the config and the code it checks
// stay next to each other in the project.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="Presentation">
                                                      <Class endsWith="Endpoint" />
                                                    </Layer>

                                                    <Layer name="Application">
                                                      <Class endsWith="Service" />
                                                    </Layer>

                                                    <Layer name="Persistence">
                                                      <Class endsWith="Repository" />
                                                    </Layer>

                                                    <AllowedDependency from="Presentation" to="Application" />
                                                    <AllowedDependency from="Application" to="Persistence" />
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.InlineXml;

public interface IOrderRepository { }

// Presentation -> Application is allowed.
public interface IOrderService { }
public class OrderEndpoint(IOrderService service) { }

// ARCH001: Presentation -> Persistence has no AllowedDependency edge.
public class AdminEndpoint(IOrderRepository repository) { }