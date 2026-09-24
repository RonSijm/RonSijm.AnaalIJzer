# IJzer

An **A**nalyzer for **N**-dimensional **A**dvanced **A**rchitectural **L**ayering.

[![NuGet](https://img.shields.io/nuget/v/RonSijm.AnaalIJzer.svg)](https://www.nuget.org/packages/RonSijm.AnaalIJzer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/RonSijm.AnaalIJzer.svg)](https://www.nuget.org/packages/RonSijm.AnaalIJzer)
[![codecov](https://codecov.io/gh/RonSijm/RonSijm.AnaalIJzer/branch/main/graph/badge.svg)](https://codecov.io/gh/RonSijm/RonSijm.AnaalIJzer)

## Introduction

I built Anaal IJzer to turn architecture rules from review comments into compiler diagnostics. You define named layers and explicit allowed dependency edges in an XML file, and the analyzer checks that types only depend on permitted layers. That is mostly it. The rest of the project is what happened after "just check a few layers" acquired tooling, diagrams, fixers, and quite a lot more XML.

## How this README is built

I keep the documentation as standalone notes in [`docs/`](../docs/) and assemble them into this README. That gives me:

- one place to edit each subject;
- the same document on GitHub, NuGet, and the Visual Studio landing page;
- no three-way contest over which almost-identical copy is currently the real one.

The compose order is defined in [`docs/_readme-order.txt`](../docs/_readme-order.txt). After changing the individual notes, run [`docs/build-readme.ps1`](../docs/build-readme.ps1) to regenerate this readme.
## Legend

| Standalone note | Use it for |
|---|---|
| [`docs/introduction.md`](../docs/introduction.md) | Project overview, naming, restaurant example domain, and Roslyn background. |
| [`docs/setup.md`](../docs/setup.md) | NuGet setup, `.anl` settings files, inline settings, and shared project configuration. |
| [`docs/configuration/ide-code-fixes.md`](../docs/configuration/ide-code-fixes.md) | Which diagnostics have IDE fixers, what they edit, and where the analyzer tests live. |
| [`docs/components/visual-studio-addon.md`](../docs/components/visual-studio-addon.md) | Visual Studio companion extension behavior, options, graph editor, and CodeLens UI. |
| [`docs/tools/arse.md`](../docs/tools/arse.md) | Arse command/TUI usage, reports, generated config, documentation, and file associations. |
| [`docs/tools/anaaltomy.md`](../docs/tools/anaaltomy.md) | Anaaltomy compiled-code statistics, SQLite storage, and Git-history trend analysis. |
| [`docs/components/wpf-graph-editor.md`](../docs/components/wpf-graph-editor.md) | Standalone WPF graph editor usage and graph image export. |
| [`docs/configuration/mental-model.md`](../docs/configuration/mental-model.md) | Beginner-friendly rule precedence and the "four questions" model. |
| [`docs/configuration/*.md`](../docs/configuration/) | Detailed settings reference for layers, dependency rules, type policies, exceptions, name rules, reports, and generated documentation. |
| [`docs/diagnostics/index.md`](../docs/diagnostics/index.md) | Diagnostic overview and links to the `ARCH_DEP_001` through `ARCH_NAME_008` pages. |
| [`docs/q-and-a.md`](../docs/q-and-a.md) | Common questions such as framework types, nested boundaries, and same-project interfaces. |
| [`docs/suppressing-violations.md`](../docs/suppressing-violations.md) | Local suppression guidance. |
| [`docs/violation-report.md`](../docs/violation-report.md) | Generated violation report output. |
| [`docs/architecture-health.md`](../docs/architecture-health.md) | Architecture health inspection output. |
| [`docs/architecture-documentation.md`](../docs/architecture-documentation.md) | Generated architecture documentation output. |
| [`docs/no-config-source.md`](../docs/no-config-source.md) | What happens when no settings source is configured. |
| [`docs/getting-started-help.md`](../docs/getting-started-help.md) | First-step guidance when starting from an existing codebase. |
| [`docs/design-generated-files.md`](../docs/design-generated-files.md) | Generated file expectations and maintenance notes. |

---

## Naming

"IJzer" is the Dutch word for iron. I - Ron, the creator of this project - have therefore decided to name it "IJzer".

---

## The problem it solves

### Meta - The Examples - Why a restaurant?

Before explaining the problem, let me explain how I'm explaining the problems. In a lot of cases I'm using 'A restaurant' as an example.

This is because architecture terms such as `Controller`, `ViewModel`, `Handler`, or `Slice` come with prior knowledge and expectations about MVC, MVVM, vertical slices, and other specific styles. Using them in the introductory examples could make an incidental name look like a rule or imply that Anaal IJzer prefers one of those architectures.

I use a restaurant as the deliberately opinionated **example domain**, not as a prescribed software architecture.

- The roles are familiar without requiring MVC, MVVM, or vertical-slice knowledge.
- `Customer`, `Waiter`, `Chef`, and `Pantry` are only layer names.
- An arrow always means **“may depend on.”** It does not describe runtime request or data flow.
- Your own configuration can use whichever layers and architectural style fit your application.

Imagine a restaurant with four roles:

- A **Customer** may ask a **Waiter** for service, but should not direct a **Chef** or enter the **Pantry**
- A **Waiter** may ask a **Chef** to prepare an order
- A **Chef** may use the **Pantry**
- Peers in the same role should not command each other unless that role explicitly allows it

Without tooling, these rules live only in code-review comments and tribal knowledge. Tribal knowledge has a habit of accepting an offer elsewhere and leaving with all of the reasoning and none of the documentation. This analyzer turns the rules into compile errors.

Architecture test projects can verify some of these concerns after a test run. Anaal IJzer reports configured violations in the editor and during compilation. Architecture tests remain useful for checks over built assemblies and external binaries.

---

## How it works

You define named layers and the edges between them in an XML file. The analyzer then checks the places where a class, record, struct, or interface can introduce another type:

- **Declarations**
  - inheritance, interface implementation, and attributes;
- **Signatures**
  - constructors, method parameters, and method returns;
- **Stored or temporary values**
  - fields, properties, and local variables;
- **Operations**
  - object creation, static member access, generic arguments, and generic service-locator calls.

When layer A introduces a dependency that its rules do not permit, the error appears on that syntax. You do not have to reconstruct it from a failed architecture test in another project.

```
Customer ──► Waiter    ✅ allowed
Waiter ──► Chef        ✅ allowed
Chef ──► Pantry        ✅ allowed

Customer ──► Chef      ❌ ARCH_DEP_001 - no AllowedDependency edge configured
Pantry ──► Chef        ❌ ARCH_DEP_004 - wrong direction (reverse of the allowed edge)
Chef ──► Chef          ❌ ARCH_DEP_005 - same layer
```

### Where it hooks into Roslyn

[Roslyn](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Roslyn-Overview.md) is the .NET compiler platform behind C# and Visual Basic. Instead of exposing only a command that turns source files into assemblies, Roslyn exposes the compiler pipeline as APIs: syntax trees represent parsed source, semantic models bind syntax to symbols and types, and a `Compilation` is an immutable snapshot of the complete program being compiled.

Anaal IJzer is a C# `DiagnosticAnalyzer`. It runs inside that compiler pipeline in Visual Studio, Rider, `dotnet build`, and CI; it is not a post-build reflection scan and does not execute application code.

```mermaid
flowchart LR
    Source["C# source"] --> Compilation["Roslyn Compilation"]
    Settings["AdditionalFiles or AssemblyMetadata"] --> Config["Architecture configuration"]
    Compilation --> Start["CompilationStartAction"]
    Start --> Syntax["Targeted SyntaxNodeAction callbacks"]
    Syntax --> Semantics["SemanticModel and ITypeSymbol resolution"]
    Config --> Rules["Layer and dependency graph"]
    Semantics --> Rules
    Rules --> Diagnostics["ARCH_* diagnostics at source locations"]
```

The integration points are:

1. [`ArchitecturalLevelAnalyzer`](../src/Main/RonSijm.AnaalIJzer.Engine/ArchitecturalLevelAnalyzer.cs) is marked with `[DiagnosticAnalyzer(LanguageNames.CSharp)]`, which makes it discoverable as a C# analyzer.
2. For each compilation snapshot, its `CompilationStartAction` reads `Architecture.anl` from Roslyn's `AdditionalFiles`, or reads inline `AssemblyMetadata("AnaalIJzerSettings", ...)`. The parsed configuration is then reused by every callback registered for that compilation.
3. It registers `SyntaxNodeAction` callbacks only for syntax that can introduce an architectural dependency: type and constructor declarations, methods, fields, properties, locals, object creation, invocations, attributes, inheritance, and static member access. Generated code is ignored, and callbacks may run concurrently.
4. [`LayerDependencyAnalyzer`](../src/Main/RonSijm.AnaalIJzer.Engine/Analysis/BoundaryRules/LayerDependencies/LayerDependencyAnalyzer.cs) uses the callback's `SemanticModel` to resolve syntax to real Roslyn symbols such as `ITypeSymbol`. This is why aliases, inferred local types, generic type arguments, implemented interfaces, and referenced types can be evaluated by their actual type identity instead of by source text alone.
5. The resolved caller and dependency symbols are matched to configured layer paths. The dependency graph evaluates the relevant boundary gates, blocked rules, site filters, recognized-dependency requirements, and forbidden patterns. A failure is returned to Roslyn with `ReportDiagnostic`, including the source location and diagnostic properties such as `Site`.
6. Configuration failures and configured cycles are reported at the end of the compilation as ARCH_CONF_003 or ARCH_CONF_006. If there is no configuration source, no dependency callbacks are registered and the analyzer remains silent.

Because the same analyzer participates in design-time and command-line compilations, the red squiggle in the editor and the error in CI come from the same rule evaluation.

---
## Why compiler-level enforcement matters

Anaal IJzer is a compile-time architecture and structural-policy guard for .NET. It overlaps with test-runner architecture checks such as NetArchTest and ArchUnitNET, static-analysis platforms such as NDepend, and the old Visual Studio layer-diagram validation. Its compiler integration also supports policies that ordinary runtime tests do not inspect.

### Architecture tests are useful, but solve a different problem

A common approach is to write a dedicated test project using a library such as [NetArchTest](https://github.com/BenMorris/NetArchTest) or [ArchUnitNET](https://archunitnet.readthedocs.io/):

```csharp
// In a test project — ArchitectureTests.cs
[Fact]
public void Presentation_Should_Not_Depend_On_Persistence()
{
    var result = Types.InAssembly(typeof(OrderEndpoint).Assembly)
        .That().ResideInNamespace("MyApp.Presentation")
        .ShouldNot().HaveDependencyOn("MyApp.Persistence")
        .GetResult();

    Assert.True(result.IsSuccessful);
}
```

That is valuable for broad assertions about an assembly or a set of published types. It is not equivalent to compiler-level enforcement:

1. **Feedback and location are different.** A test reports from the test project when the test suite runs. Anaal IJzer reports on the exact source construct during design-time analysis and compilation, so the editor squiggle and CI error point to the same dependency, return expression, or declaration.

2. **Behavioural tests only see executed paths.** A `return null`, a sentinel return value, or a `throw` deep in a branch can remain invisible until a test happens to execute that path. Static type-level architecture tests can assert a relationship between types, but they do not automatically inspect every method body and every relevant syntax site.

3. **Complete source inspection needs a compiler host.** A test suite could add custom Roslyn or IL inspection for every return, invocation, generic argument, inheritance site, or declaration it cares about. That is effectively building a compiler inspection in a test runner. Anaal IJzer is already hosted at that point: Roslyn visits every configured matching site in the compilation, including code that no test executes.

4. **Policy is distinct from behaviour.** A failing behaviour test says a scenario no longer works. A failing architectural policy says the code shape itself is not permitted, even when the scenario still works perfectly. Both are important, but they should be visible and owned separately.

5. **Configuration is the policy surface.** With `Architecture.anl`, layer relationships, type policies, site restrictions, and structural observations are explicit configuration. Changing the policy does not require inventing another test method or burying the rule in test code.

### What Anaal IJzer adds

Anaal IJzer uses Roslyn's semantic model to resolve the symbols behind the source. That gives the rules a few useful properties:

- aliases and fully qualified names resolve to the same symbol;
- inferred locals still have a real type;
- generic arguments, implemented interfaces, and attributes are inspected semantically;
- nested boundaries are evaluated from their actual configured layer paths;
- method-body policies apply even when no test happens to execute that branch.

For example:

- A [`ReturnValuePolicy`](configuration/return-value-policies.md) can reject a direct `return null`, an empty string, an enum-zero sentinel, or the unchanged result of a method annotated as optional.
- A [`ForbiddenOperations` policy](configuration/forbidden-operation-policies.md) can reject `DateTime.UtcNow` or `Task.Wait()` while leaving other members of the same framework type available.

Runtime coverage cannot prove a source-site rule unless it executes every relevant path. Equivalent coverage requires static inspection. Compiler analysis therefore addresses a different class of policy from architecture tests over built assemblies.

### Complementary tools

Architecture tests still have a place for broad checks over shipped assemblies, external binaries, or intentional test-suite-level assertions. Behavioural and integration tests remain essential for proving that the application works. Anaal IJzer complements them by making configured structural and semantic policies part of ordinary compilation, with immediate feedback at the offending line.
