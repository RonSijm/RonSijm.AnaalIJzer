// ReSharper disable All - Justification: Example File
namespace Example.RequiredRecognizedDependencySites;

public sealed class UnknownConstructorDependency { }

// ARCH002 at Constructor.
public sealed class ConstructorCaller(UnknownConstructorDependency dependency) { }

public sealed class UnknownMethodDependency { }

// ARCH002 at Method.
public sealed class MethodCaller
{
    public void Use(UnknownMethodDependency dependency) { }
}

public sealed class UnknownReturnDependency { }

// ARCH002 at MethodReturn.
public sealed class MethodReturnCaller
{
    public UnknownReturnDependency Get()
    {
        return null!;
    }
}

public sealed class UnknownFieldDependency { }

// ARCH002 at Field.
public sealed class FieldCaller
{
    public readonly UnknownFieldDependency Dependency = null!;
}

public sealed class UnknownPropertyDependency { }

// ARCH002 at Property.
public sealed class PropertyCaller
{
    public UnknownPropertyDependency Dependency { get; set; } = null!;
}