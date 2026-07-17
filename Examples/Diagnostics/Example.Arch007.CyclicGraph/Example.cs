// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels enforceAcyclic="true">
                                                    <Layer name="Ordering"><Class typeName="OrderingService" /></Layer>
                                                    <Layer name="Inventory"><Class typeName="InventoryService" /></Layer>
                                                    <Layer name="Billing"><Class typeName="BillingService" /></Layer>
                                                    <AllowedDependency from="Ordering" to="Inventory" />
                                                    <AllowedDependency from="Inventory" to="Billing" />
                                                    <AllowedDependency from="Billing" to="Ordering" />
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.Arch007.CyclicGraph;

// ARCH007: the configured permissions form Ordering -> Inventory -> Billing -> Ordering.
public sealed class OrderingService;
public sealed class InventoryService;
public sealed class BillingService;