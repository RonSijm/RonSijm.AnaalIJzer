## Solution topology

`SolutionTopology` is the solution-wide counterpart to `ProjectArchitecture`.

Use it when the architecture rule is about the shape of an entire loaded solution:

- several projects belong to one logical module;
- only some modules may reference one another;
- a direct project reference needs evidence in a solution report, not a compiler diagnostic;
- the configured module graph must remain acyclic.

`SolutionTopology` is intentionally evaluated by Arse through `MSBuildWorkspace`, not by the compiler analyzer. A normal project build remains self-contained and does not need to load its sibling projects.

### Example

```xml
<ArchitecturalLevels>
  <SolutionTopology requireRecognizedProjects="true"
                    enforceAcyclic="true">
    <Module name="DiningRoom">
      <Project endsWith=".Web" />
    </Module>
    <Module name="Kitchen">
      <Project endsWith=".Application" />
    </Module>
    <Module name="Pantry">
      <Project endsWith=".Infrastructure" />
    </Module>

    <AllowedModuleReference from="DiningRoom" to="Kitchen" />
    <BlockedModuleReference from="Kitchen" to="Pantry" />
  </SolutionTopology>
</ArchitecturalLevels>
```

The restaurant names are only a readable domain metaphor. The arrows mean “may reference,” never runtime request or data flow.

With this configuration, `Shop.Web -> Shop.Application` is allowed and `Shop.Application -> Shop.Infrastructure` produces `ARCH_SOL_001` during an explicitly enforced solution inspection.

### Modules and matchers

Each `Module` has a unique name and one or more `Project` matchers. `Project` uses the same textual matcher attributes as `ProjectArchitecture`:

| Attribute | Meaning |
|---|---|
| `typeName` / `exactName` | Exact project-name match without `.csproj` |
| `startsWith` | Prefix match |
| `endsWith` | Suffix match |
| `contains` | Substring match |
| `regex` | Regular expression |

Attributes on one `Project` matcher are combined with AND semantics. Multiple `Project` matchers in a module are alternatives. If more than one module matches, the first module in configuration order is authoritative.

### Module-reference rules

`AllowedModuleReference` and `BlockedModuleReference` use named modules or `*`:

```xml
<AllowedModuleReference from="DiningRoom" to="Kitchen" />
<BlockedModuleReference from="Kitchen" to="Pantry" />
<AllowedModuleReference from="Tests" to="*" />
```

Blocked rules win. Like `ProjectArchitecture`, a source module enters allowlist mode only when it has a matching allowed rule. A module with only blocked rules remains blocklist-only, which makes it possible to introduce topology checks gradually.

`requireRecognizedProjects` defaults to `false`. When enabled, each endpoint of an observed direct project reference must match a module. `enforceAcyclic` defaults to `false`; when enabled, a configured cycle among explicit allowed module rules produces `ARCH_SOL_006`.

### Run it deliberately

```cmd
arse inspect --solution src\Shop\Shop.slnx --enforce-topology --output build\Artifacts\architecture-health.md --force
arse inspect --solution src\Shop\Shop.slnx --enforce-topology --output build\Artifacts\architecture-health.json --force
```

The Markdown report is intended for review. Choosing a `.json` output path writes the same ordered findings as machine-readable evidence, including source and target project paths, module names, the reason, and the matching rule location. Headless Arse returns exit code `3` when the inspection finds a problem.

The repository includes a reusable [Solution topology GitHub workflow](../../.github/workflows/solution-topology.yml). Call it from a product workflow or dispatch it with a solution path; it restores and builds Arse, uploads both evidence files, and fails only after those artifacts are available.

`ARCH_SOL_001` and `ARCH_SOL_006` are report finding codes, not compiler `ARCH` diagnostics. This distinction is intentional: opening an entire solution is a tooling operation, while an analyzer must stay fast and safe inside a normal project compilation.

### Viewing the topology

Open a configured solution in the standalone WPF graph editor or use `Extensions > IJzer > Show Dependency Graphs` in Visual Studio. The graph renders `SolutionTopology` as a separate, read-only group: each configured module is a node, configured module rules are connections, and loaded direct project references appear as evidence connections. A permitted observed reference is muted; a `ARCH_SOL_001` violation is highlighted for investigation.

That view deliberately does not expose drag-to-edit controls for modules or module rules. `SolutionTopology` is currently authored in `Architecture.anl`, and keeping the diagram read-only prevents a user from assuming that moving a module changes the solution policy. The normal layer graph remains editable when the same configuration also contains `<Layer>` rules.

See [`Example.SolutionTopology`](../../Examples/Scenarios/Example.SolutionTopology) for a compact multi-project example whose normal build succeeds and whose explicit solution inspection fails with one intentional `ARCH_SOL_001`.
