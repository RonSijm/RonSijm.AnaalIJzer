## ARCH011: Package Reference Violation

`ARCH011` reports an illegal NuGet package reference under `ProjectArchitecture`.

This is a project-topology and dependency-policy rule, not a type-usage rule.

### Example

```text
Project 'Shop.Domain' (project group Domain) may not reference package 'Microsoft.Extensions.Logging' 9.0.0: the package does not match the Allowed package list for project group 'Domain'
```

### Typical Causes

- a project group has an allow-list package policy and the package does not match any allowed matcher
- a package matches a forbidden package matcher
- `requireRecognizedProjects="true"` is enabled and the source project matches no `ProjectGroup`
- a direct package reference is legal but its transitive package is still denied when `includeTransitive="true"` is active

### Typical Fixes

- remove the package reference from the project
- move the dependency to a project group where that package belongs
- widen the allowed package list if the dependency is intentional
- re-scope the forbidden package matcher if it is too broad
- classify the source project with a `ProjectGroup` when recognition is the actual issue

### IDE Fix Support

For the deterministic allow-list case, the config fixer layer can append an exact matcher such as:

```xml
<Package exactName="Microsoft.Extensions.Logging" />
```

That fix is only offered when the current violation is specifically an allowed-list miss. If a forbidden matcher rejected the package, the fixer does not guess by weakening that rule - somebody wrote it deliberately, and undoing it should take at least as much thought.

Because `ARCH011` is reported at compilation end, host UX depends on how the IDE surfaces `Location.None` diagnostics. The edit logic itself is covered by `PackagePolicyCodeFixTests.cs`.

### Not The Same As

- `ARCH003`: forbidden type usage in C# code
- `ARCH010`: illegal direct project reference
- `ARCH001`: illegal type dependency between layers
