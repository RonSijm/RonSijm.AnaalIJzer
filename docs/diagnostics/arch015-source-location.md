## ARCH015 - Source-location violation

`ARCH015` means a type matched a layer, but one of its source declarations is not in an allowed owned location for that layer.

Example message:

```text
'CandyOrderingService' belongs to layer 'Ordering/Application' but source file 'Infrastructure/CandyOrderingService.cs' does not match an allowed SourceLocations rule for layer 'Ordering'
```

Typical causes:

- the namespace still matches the intended layer, but the file was moved into the wrong folder;
- a partial type was split across owned and unowned locations;
- a configuration-relative rule points at the wrong base folder;
- an assembly-constrained source rule matches the folder but not the project assembly.

Typical fixes:

- move the file into the owned folder or project;
- tighten or correct the `<SourceLocations>` patterns;
- split mixed-responsibility partial declarations;
- if the layout is intentional, add an explicit `<Source>` rule that documents it.

#### IDE code fix

The IDE can add an exact `<Source exactName="..."/>` matcher for the reported file path to the owning layer's `<SourceLocations>` block. For inline metadata config, the assembly attribute is rewritten in place.

Important: folders do not classify layers by themselves. `ARCH015` only runs after the type has already been matched into a layer by the normal layer matchers.

See [`Example.SourceLocations`](../../Examples/Features/Example.SourceLocations) for a small build-verified sample.
