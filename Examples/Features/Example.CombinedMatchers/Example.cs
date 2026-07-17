// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="DataContracts">
    <Class endsWith="Repository" typeKind="Interface" />
  </Layer>

  <Layer name="DataImplementation">
    <Class endsWith="Repository" typeKind="Class" />
  </Layer>

  <AllowedDependency from="DataImplementation"
                     to="DataContracts"
                     allowedSites="InterfaceImplementation" />
</ArchitecturalLevels>
""")]

namespace Example.CombinedMatchers;

public interface IExampleRepository { }

// Allowed: class and interface share a suffix but resolve to different layers.
public class ExampleRepository : IExampleRepository { }

// ARCH005: both types are classes in DataImplementation, and only interface implementation is allowed.
public class CachedRepository : ExampleRepository { }