## Transitive API exposure

Direct API checks stop at the declared signature type. A contract can therefore look safe while its public object graph exposes a repository-owned type one step later:

```text
CandyOrderingService.OrderRawLolly
    -> CandyReceipt.RawQuery
    -> LollyQueryable
```

Add `<TransitiveExposure>` to an existing `<ApiSurface>` to inspect that object graph. A query surface rarely gets published through a receipt property on purpose; it arrives because that property was convenient on a Tuesday.

```xml
<Layer name="Application">
  <Class endsWith="Service" />

  <ApiSurface description="Public application APIs expose contracts only.">
    <TransitiveExposure
        maxDepth="3"
        description="Follow public contract members for hidden repository surfaces." />
    <AllowedLayer path="/Contracts" />
    <BlockedLayer path="/RepositoryQuerySurface" />
  </ApiSurface>
</Layer>
```

`maxDepth` defaults to `3` and accepts values from `1` through `10`. Traversal is opt-in: omitting `<TransitiveExposure>` preserves direct ARCH009 behavior.

The analyzer performs a breadth-first traversal and reports the shortest forbidden path. It follows externally visible fields, events, properties, indexers, method and constructor signatures, base types, interfaces, constraints, arrays, tuples, nullable values, delegates, and generic arguments. Private implementation details are ignored.

Traversal is:

- bounded by `maxDepth`;
- cached per compilation;
- cycle-safe for self-referential and mutually recursive contracts;
- cancellable;
- stopped at unrecognized types unless `requireRecognizedTypes="true"` makes the unrecognized exposure itself invalid.

The nested member's own API site is evaluated against `allowedSites` and `blockedSites`. A property reached through a public contract therefore uses `Property`, even when the root contract was exposed at `MethodReturn`.

A directly forbidden signature still reports ARCH009 only. ARCH014 is reserved for a permitted root type whose public object graph reaches a forbidden type.

**Example project:** [`Example.Arch014.TransitiveExposure`](../../Examples/Diagnostics/Example.Arch014.TransitiveExposure)
