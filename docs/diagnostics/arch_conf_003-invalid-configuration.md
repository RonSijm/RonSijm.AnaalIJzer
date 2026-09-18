### ARCH_CONF_003 - Invalid architecture configuration

Reported when settings cannot be evaluated reliably: malformed or schema-invalid XML, missing includes, duplicate layers, invalid or ambiguous matchers, invalid site filters, or dependency rules that reference unknown layers. The analyzer no longer becomes silently inactive when configuration parsing fails, because a misspelled layer name used to produce the same clean build as a flawless codebase - flattering, but not informative.

**Example project:** [`Example.Arch_CONF_003.UnknownLayer`](../../Examples/Diagnostics/CONF/Example.Arch_CONF_003.UnknownLayer)

#### Real-world uses

- Fail CI when a layer was renamed but an edge, include, or policy still references the old name.
- Catch a malformed drop-in `.anl` rule pack before it quietly disables the architectural guard it was meant to add.
