### `description` attributes

Every XML element that participates in the ruleset can carry a `description` attribute: `<ArchitecturalLevels>`, `<Include>`, `<Layer>`, `<Class>`, `<Namespace>`, `<Assembly>`, `<Allowed>`, `<Forbidden>`, `<Exceptions>`, `<Fix>`, `<AllowedDependency>`, `<BlockedDependency>`, `<NameRules>`, `<RequireMatchingNames>`, `<RequireDeclarationNameMatchesType>`, `<VisibilityPolicy>`, `<InheritancePolicy>`, `<ApiSurface>`, `<AllowedLayer>`, `<BlockedLayer>`, `<Type>`, `<NestedType>`, `<Constructor>`, `<Method>`, `<Property>`, `<Field>`, `<Event>`, `<Operator>`, `<Conversion>`, `<Name>`, `<Source>`, `<Target>` and `<Allow>`. Descriptions do not affect diagnostics. They exist so generated documentation can explain why a rule exists while preserving the same order as the XML.

```xml
<Layer name="QuerySurface"
       description="Repository-owned fluent query builders that must be projected before leaving repository-owned code.">
  <Class endsWith="Query"
         description="Query objects are transient access points, not application dependencies." />
</Layer>

<AllowedDependency from="Persistence" to="QuerySurface"
                   allowedSites="MethodReturn, New"
                   description="Repositories may create and return query surfaces as fluent access points." />
```

**Example project:** [`Example.DocumentationDemo`](../../Examples/Documentation/Example.DocumentationDemo)

<details>
<summary>Dependency graph</summary>

<img src="../../Examples/Documentation/Example.DocumentationDemo/Example.DocumentationDemo-Graph.png" alt="Example.DocumentationDemo dependency graph">

</details>


---
