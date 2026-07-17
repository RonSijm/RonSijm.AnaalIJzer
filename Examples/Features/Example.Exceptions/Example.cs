// ReSharper disable All - Justification: Example File
// <Exceptions> — grandfather pre-existing violations while still blocking new ones.
//
// The XML config exempts every type starting with "Legacy" from the
// forbidden <Class endsWith="Store"> rule, and exempts
// InMemoryFakeOrderRepository (by exact name) from the Repository layer.

// ✅ Legacy 'Store' is exempted by <Class startsWith="Legacy"> — no diagnostic.

using System.Reflection;
using Example.Exceptions;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
                                                   <ArchitecturalLevels>

                                                     <Forbidden>
                                                       <Class endsWith="Store" comment="Persistence types must use the Repository suffix.">
                                                         <Fix Rename="Repository" />
                                                         <Exceptions>
                                                           <Class startsWith="Legacy" />
                                                         </Exceptions>
                                                       </Class>
                                                     </Forbidden>

                                                     <Layer name="Application">
                                                       <Class endsWith="Service" />
                                                       <Class endsWith="Manager" />
                                                       <Class endsWith="Coordinator" />
                                                     </Layer>

                                                     <Layer name="Repository">
                                                       <Class endsWith="Repository">
                                                         <Exceptions>
                                                           <Class typeName="{nameof(InMemoryFakeOrderRepository)}" />
                                                         </Exceptions>
                                                       </Class>
                                                     </Layer>

                                                     <AllowedDependency from="Application" to="Repository" />

                                                   </ArchitecturalLevels>
                                                   """)]

namespace Example.Exceptions;

public class LegacyOrderStore { }
public class OrderHistoryManager(LegacyOrderStore store) { }

// ARCH003: OrderStore matches the forbidden rule and is not exempted.
// The Legacy exception scopes the carve-out narrowly.
public class OrderStore { }
public class OrderManager(OrderStore store) { }

// InMemoryFakeOrderRepository is exempted from the Repository layer by exact
// typeName, so it falls back to unlayered and no recognition requirement raises anything.
public class InMemoryFakeOrderRepository { }
public class FakeManager(InMemoryFakeOrderRepository fake) { }

// A regular Repository still works because Application -> Repository is configured.
public class OrderRepository { }
public class OrderService(OrderRepository repository) { }