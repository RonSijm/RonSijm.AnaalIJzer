// ReSharper disable All - Justification: Example File
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <ExceptionPolicy requireOwner="true"
                   description="Temporary exceptions must name who owns them." />

  <Layer name="Application">
    <Class endsWith="Manager">
      <Exceptions>
        <Class typeName="LegacyManager" />
      </Exceptions>
    </Class>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.ExceptionPolicy;

// ARCH017: the exception is missing required owner metadata.
public class LegacyManager { }
