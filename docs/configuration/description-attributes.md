### `description` attributes

Every XML element that participates in the ruleset can carry a `description` attribute. That includes:

- **Structure**
  - `<ArchitecturalLevels>`, `<Include>`, and `<Layer>`;
- **Matchers and exceptions**
  - `<Class>`, `<Namespace>`, `<Assembly>`, `<Type>`, `<NestedType>`, `<ContainingType>`, `<Member>`, `<Name>`, `<Source>`, `<Target>`, `<Exceptions>`, and `<Fix>`;
- **Type and dependency policies**
  - `<Allowed>`, `<Forbidden>`, `<AllowedDependency>`, `<BlockedDependency>`, `<ApiSurface>`, `<AllowedLayer>`, and `<BlockedLayer>`;
- **Namespace and operation contracts**
  - `<NamespaceHierarchyPolicy>`, `<BlockedRelation>`, `<Operations>`, `<Operation>`, `<Owner>`, `<Request>`, `<Response>`, and `<EntryPoint>`;
- **Name, visibility, inheritance, and return policies**
  - `<NameRules>`, `<RequireMatchingNames>`, `<RequireDeclarationNameMatchesType>`, `<Allow>`, `<VisibilityPolicy>`, `<InheritancePolicy>`, `<ReturnValuePolicy>`, and `<AllowedReturn>`;
- **Operation policies**
  - `<ForbiddenOperations>`, `<ForbiddenOperation>`, `<BehavioralOperations>`, `<RequiredOperation>`, `<RequiredOperationBefore>`, `<ForbiddenOperationAfter>`, `<MaximumOperationCount>`, `<DeclarationMatcher>`, `<BeforeOperation>`, `<AfterOperation>`, and `<OperationMatcher>`;
- **Declaration matchers**
  - `<Constructor>`, `<Method>`, `<Property>`, `<Field>`, `<Event>`, `<Operator>`, and `<Conversion>`.

Descriptions do not affect diagnostics. They are the cheapest place to record intent: without one, a future reviewer has to guess why a rule exists, and guesswork usually resolves in favour of deleting it.

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
