// ReSharper disable All - Justification: Example File
using System;

namespace Example.Arch001.NonConstructorInjection;

// ARCH001: finding the chef through a hidden lookup still bypasses the waiter.
public class ServiceLocatorCustomer
{
    public void Run(IServiceProvider services)
    {
        var chef = services.GetRequiredService<IChef>();
        _ = chef;
    }
}

public static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider services) where T : class
    {
        return (T)services.GetService(typeof(T))!;
    }
}