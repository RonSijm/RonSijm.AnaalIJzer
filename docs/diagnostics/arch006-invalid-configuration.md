### ARCH006 - Invalid architecture configuration

Reported when settings cannot be evaluated reliably: malformed or schema-invalid XML, missing includes, duplicate layers, invalid or ambiguous matchers, invalid site filters, or dependency rules that reference unknown layers. The analyzer no longer becomes silently inactive when configuration parsing fails.

**Example project:** [`Example.Arch006.UnknownLayer`](../../Examples/Diagnostics/Example.Arch006.UnknownLayer)
