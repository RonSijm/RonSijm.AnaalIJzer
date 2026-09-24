### ARCH_TYPE_001 - Type policy violation

Reported when a dependency type matches an applicable `<Forbidden>` pattern or does not match an applicable `<Allowed>` list. The two causes read similarly in an error list but mean different things: one type is specifically unwelcome, while the other simply never made the guest list. If a `<Fix Rename="…">` is configured on a forbidden pattern, Visual Studio and Rider offer a rename code fix. Forbidden-rule matches can also add the type to that rule's `<Exceptions>` block. For allow-list failures, the IDE can add an exact `<Class typeName="..."/>` matcher to every applicable `<Allowed>` list.

**Example output:**
```
error ARCH_TYPE_001: 'ReportingService' (layer Application) may not use 'LegacyOrderStore':
  the type matches a global <Forbidden> rule: Persistence types must use the Repository suffix.
```

#### Real-world uses

- Require persistence abstractions to use a `Repository` convention and reject legacy `Store` or `Manager` types at the dependency site.
- Keep selected framework types, such as EF Core attributes or transport DTOs, out of a domain boundary with a scoped `<Forbidden>` policy.
