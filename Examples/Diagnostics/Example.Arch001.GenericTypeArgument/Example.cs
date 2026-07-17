// ReSharper disable All - Justification: Example File
// ARCH001: generic type arguments are inspected. Wrapping a forbidden
// dependency in Lazy<>, IEnumerable<>, Func<>, ... does not hide it from
// the analyzer — the inner type is still resolved to its layer and the
// usual edge rules apply.

using System;
using System.Collections.Generic;
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

namespace Example.Arch001.GenericTypeArgument;

public interface IWaiter { }
public interface IChef { }

// Customer -> Waiter is allowed.
public class HungryCustomer(IWaiter waiter) { }

// ARCH001: Lazy<IChef> still bypasses the waiter.
// Asking the chef later is still asking the chef directly.
public class PatientCustomer(Lazy<IChef> chef) { }

// ARCH001: IEnumerable<IChef> still contains direct chef dependencies.
// A group of chefs does not become a waiter.
public class GroupCustomer(IEnumerable<IChef> chefs) { }

// ARCH001: Func<IChef> still resolves to a chef.
// Deciding to find the chef later does not restore the missing waiter.
public class FutureCustomer(Func<IChef> chefFactory) { }