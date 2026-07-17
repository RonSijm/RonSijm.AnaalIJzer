// ReSharper disable All - Justification: Example File
using System;

namespace Example.RequiredRecognizedDependencySites;

public sealed class UnknownLocalDependency { }

// ARCH002 at Local.
public sealed class LocalCaller
{
    public void Run()
    {
        UnknownLocalDependency dependency = null!;
        _ = dependency;
    }
}

public sealed class UnknownNewDependency { }

// ARCH002 at New.
public sealed class NewCaller
{
    public void Run()
    {
        _ = new UnknownNewDependency();
    }
}

public sealed class UnknownGenericInvocationDependency { }

// ARCH002 at GenericInvocation.
public sealed class GenericInvocationCaller
{
    public void Run()
    {
        _ = Resolve<UnknownGenericInvocationDependency>();
    }

    private static T Resolve<T>() where T : class
    {
        return null!;
    }
}

public sealed class UnknownGenericArgumentDependency { }

// Lazy is recognized, but ARCH002 is reported for its GenericArgument.
public sealed class GenericArgumentCaller(Lazy<UnknownGenericArgumentDependency> dependency) { }