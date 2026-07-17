// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="DataAbstraction">
                                                      <Class endsWith="Repository" />
                                                    </Layer>

                                                    <AllowedDependency from="DataAbstraction"
                                                                       to="DataAbstraction"
                                                                       allowedSites="InterfaceImplementation" />
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.SameLayerInheritance;

public interface IExampleRepository { }

// Allowed: implementing an interface is an InterfaceImplementation-site dependency.
public class ExampleRepository : IExampleRepository { }

// ARCH005: the self-edge permits InterfaceImplementation, not Constructor.
public class ReportingRepository(IExampleRepository repository) { }