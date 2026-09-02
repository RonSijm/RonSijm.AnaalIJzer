## ARCH017 - Architecture exception requires review

`ARCH017` is a warning about the exception itself, not about the original architectural rule.

It appears when an exception matcher:

- is missing required metadata from `<ExceptionPolicy>`;
- has an invalid `expiresOn` date;
- has already expired;
- is close to expiry;
- or, in Arse health inspection, is stale and matches no type in the inspected scope.

Example warning:

```text
Architecture exception for Class 'typeName="LegacyManager"' is missing required owner metadata
```

Typical causes:

1. The config has `<ExceptionPolicy requireOwner="true" />`, but the exception has no `owner`.
2. The exception expired before Sunday, July 26, 2026.
3. The exception was once needed, but the type it named no longer exists.

Important semantics:

- Missing or invalid required metadata makes the exception inactive.
- Expired exceptions are ignored by the matcher engine.
- If an expired exception used to suppress another diagnostic, that original diagnostic can reappear.
- Expiring-soon exceptions still suppress the original diagnostic until their expiry date.

A stale exception naming a type that was deleted two refactors ago protects nothing; it only makes the config longer and the next reader more nervous.

The warning carries these properties:

- `ExceptionMatcherKind`
- `ExceptionMatcherLabel`
- `ExceptionReason`
- `ExceptionOwner`
- `ExceptionExpiresOn`
- `ExceptionStatus`

See also:

- [`../configuration/exception-policy.md`](../configuration/exception-policy.md)
- [`../../Examples/Features/Example.ExceptionPolicy/Example.cs`](../../Examples/Features/Example.ExceptionPolicy/Example.cs)
