using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application"><Class typeName="OrderService" /></Layer>
  <AllowedDependency from="Application" to="TypoRepository" />
</ArchitecturalLevels>
""")]

// ARCH006: TypoRepository is not a declared layer, so the edge cannot be evaluated.
public sealed class OrderService;
