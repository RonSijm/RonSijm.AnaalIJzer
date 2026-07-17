### ARCH003 - Type policy violation

Reported when a dependency type matches an applicable `<Forbidden>` pattern or does not match an applicable `<Allowed>` list. If a `<Fix Rename="…">` is configured on a forbidden pattern, Visual Studio and Rider will offer a one-click rename code-fix. When a forbidden rule comes from `Architecture.anl`, a second "Add '`TypeName`' to exceptions" code action is offered. An allow-list failure has no single originating matcher to except, so that code action is not offered.

**Example output:**
```
error ARCH003: 'ReportingService' (layer Application) may not use 'LegacyOrderStore':
  the type matches a global <Forbidden> rule: Persistence types must use the Repository suffix.
```
