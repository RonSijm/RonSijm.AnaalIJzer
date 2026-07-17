// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
                                                  <ArchitecturalLevels>
                                                    <Layer name="Caller">
                                                      <Class typeName="OrderRecordCaller" />
                                                      <Class typeName="OrderStructCaller" />
                                                      <Class typeName="IOrderInterfaceCaller" />
                                                    </Layer>
                                                    <Layer name="Repository"><Class typeName="OrderRepository" /></Layer>
                                                  </ArchitecturalLevels>
                                                  """)]

namespace Example.NonClassCallers;

public sealed class OrderRepository;

// ARCH001: records are architectural callers too.
public sealed record OrderRecordCaller
{
    public OrderRepository Get()
    {
        return null!;
    }
}

// ARCH001: structs are architectural callers too.
public struct OrderStructCaller
{
    public OrderRepository Get()
    {
        return null!;
    }
}

// ARCH001: interface API surfaces are architectural callers too.
public interface IOrderInterfaceCaller
{
    OrderRepository Get();
}