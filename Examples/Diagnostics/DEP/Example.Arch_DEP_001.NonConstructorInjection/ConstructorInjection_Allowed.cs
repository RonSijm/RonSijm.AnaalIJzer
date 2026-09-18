// ReSharper disable All - Justification: Example File
// Customer -> Waiter is the allowed baseline.
namespace Example.Arch_DEP_001.NonConstructorInjection;

public class HungryCustomer(IWaiter waiter) { }