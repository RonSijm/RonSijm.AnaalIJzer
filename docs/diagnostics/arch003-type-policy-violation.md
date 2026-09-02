### ARCH003 - Type policy violation

Reported when a dependency type matches an applicable `<Forbidden>` pattern or does not match an applicable `<Allowed>` list. If a `<Fix Rename="…">` is configured on a forbidden pattern, Visual Studio and Rider will offer a one-click rename code-fix. Forbidden-rule matches can also add the type to that rule's `<Exceptions>` block. Allow-list failures use a different fixer: the IDE can add an exact `<Class typeName="..."/>` matcher to every applicable `<Allowed>` list.

**Example output:**
```
error ARCH003: 'ReportingService' (layer Application) may not use 'LegacyOrderStore':
  the type matches a global <Forbidden> rule: Persistence types must use the Repository suffix.
```
