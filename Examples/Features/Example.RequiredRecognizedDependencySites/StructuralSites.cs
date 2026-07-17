// ReSharper disable All - Justification: Example File
using System;

namespace Example.RequiredRecognizedDependencySites;

public class UnknownBaseDependency { }

// ARCH002 at Inheritance.
public sealed class InheritanceCaller : UnknownBaseDependency { }

public interface IUnknownInterfaceDependency { }

// ARCH002 at InterfaceImplementation.
public sealed class InterfaceImplementationCaller : IUnknownInterfaceDependency { }

public sealed class UnknownAttributeDependency : Attribute { }

// ARCH002 at Attribute.
[UnknownAttributeDependency]
public sealed class AttributeCaller { }

public static class UnknownStaticDependency
{
    public static int Value
    {
        get { return 1; }
    }
}

// ARCH002 at StaticMember.
public sealed class StaticMemberCaller
{
    public int Get()
    {
        return UnknownStaticDependency.Value;
    }
}