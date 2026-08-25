## Source locations

`<SourceLocations>` lets a layer say where its types are allowed to live on disk. This is separate from layer matching:

- layer matchers answer "what role does this type have?";
- source locations answer "does that role live in the right project or folder?"

Restaurant version:

- the type may still be a `Chef`;
- `SourceLocations` checks whether that chef is actually in the kitchen, not wandering around the pantry office.

Example:

```xml
<Layer name="Ordering">
  <Namespace startsWith="Shop.Ordering" />

  <SourceLocations relativeTo="Project">
    <Source startsWith="Ordering/" />
    <Source startsWith="Contracts/Ordering/" assemblyName="Shop.Contracts" />
  </SourceLocations>
</Layer>
```

`<Source>` uses the same textual matcher attributes as other text-based matching:

| Attribute | Meaning |
|---|---|
| `typeName` | Exact normalized path text |
| `exactName` | Exact normalized path text |
| `startsWith` | Path prefix |
| `endsWith` | Path suffix |
| `contains` | Path fragment |
| `regex` | Regular expression against the normalized path |
| `assemblyName` | Optional exact compilation assembly name that must also match |

Attributes on one `<Source>` are combined with AND semantics. Separate `<Source>` elements are alternatives.

### `relativeTo`

| Value | Base path |
|---|---|
| `Project` | `MSBuildProjectDirectory` |
| `Configuration` | The physical `.anl` file that declared the rule |
| `Absolute` | The full normalized source path |

Default: `Project`.

`Configuration` is only valid for file-based settings. Inline `AssemblyMetadata("AnaalIJzerSettings", ...)` has no physical settings directory, so that combination reports `ARCH006`.

### Partial types

Every declaration of a partial type must satisfy every applicable source-location policy. One correctly placed file does not hide one misplaced file.

### Nested layers

Source-location policies are cumulative through ancestry:

- a parent layer policy still applies to child layers;
- a child can add more specific ownership rules;
- a child cannot relax a parent source-location rule.

### Example

See [`Example.SourceLocations`](../../Examples/Features/Example.SourceLocations), where one `SweetShop.Ordering` type is correctly placed under `Ordering/` and another is deliberately misplaced under `Infrastructure/`.
