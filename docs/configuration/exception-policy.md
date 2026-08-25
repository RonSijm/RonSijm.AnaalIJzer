## ExceptionPolicy

`<ExceptionPolicy>` makes matcher exceptions temporary and reviewable.

Without it, `<Exceptions>` keep their existing behavior:

- no metadata is required;
- no warning is reported;
- exceptions stay active until someone edits the config.

With it, you can require metadata on every matcher directly inside an `<Exceptions>` block:

```xml
<ArchitecturalLevels>
  <ExceptionPolicy requireReason="true"
                   requireOwner="true"
                   requireExpiresOn="true"
                   warnBeforeDays="14" />

  <Layer name="Application">
    <Class endsWith="Manager">
      <Exceptions>
        <Class typeName="LegacyManager"
               reason="Migration tracked in ORDERING-142"
               owner="Ordering Team"
               expiresOn="2026-10-31" />
      </Exceptions>
    </Class>
  </Layer>
</ArchitecturalLevels>
```

Supported attributes:

| Attribute | Default | Meaning |
|---|---|---|
| `requireReason` | `false` | Require a non-empty `reason` attribute on exception matchers |
| `requireOwner` | `false` | Require a non-empty `owner` attribute on exception matchers |
| `requireExpiresOn` | `false` | Require an `expiresOn="yyyy-MM-dd"` attribute on exception matchers |
| `warnBeforeDays` | `14` | Emit `ARCH017` when an exception expires within this many days |

Behavior:

- Missing required metadata reports `ARCH017`.
- Invalid `expiresOn` reports `ARCH017`.
- Expired exceptions report `ARCH017` and fail closed.
- Expiring-soon exceptions report `ARCH017` but remain active.
- Stale exceptions are reported by Arse health inspection, not by normal project compilation.

See also:

- [`exceptions.md`](exceptions.md)
- [`../diagnostics/arch017-exception-review.md`](../diagnostics/arch017-exception-review.md)
- [`../../Examples/Features/Example.ExceptionPolicy/Example.cs`](../../Examples/Features/Example.ExceptionPolicy/Example.cs)
