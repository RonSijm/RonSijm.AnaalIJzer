# Visual Studio Screenshots

This directory contains the screenshots currently rendered in the main README's Visual Studio companion section. It lists only committed images.

Use [`Example.VisualStudioSiteDiagnostics`](../../Documentation/Example.VisualStudioSiteDiagnostics) for the all-site captures. Its one clean source file contains every supported site with no intended analyzer errors.

## Declaration layer information

| File | Capture |
|---|---|
| `editor-settings.png` | `AnaalIJzer > Editor` settings with declaration-layer controls visible. |
| `layer-badge.png` | Only `Show layer badges`, with one canonical layer-path badge. |
| `layer-codelens.png` | Only `Show layer metadata above declarations`, with its clickable summary. |
| `not-in-layer-badge.png` | `Show layer badges when not in layer`, with an unmatched type. |
| `layer-gutter-glyph.png` | Only `Gutter glyphs`, with the marker beside a layered type. |
| `layer-block-highlight.png` | Only `Highlight code in layer`, with the complete declaration block visible. |
| `layer-badge-hover-info.png` | Layer CodeLens and QuickInfo with ancestry, call chain, and incoming/outgoing layers. |

## Dependency-site labels

| File | Capture |
|---|---|
| `site-layer-information-all-sites.png` | `Show all layer information` enabled, with every site visible in the one-file Visual Studio demonstration. |
| `site-diagnostics-constructor.png` | Constructor Site Diagnostics on an `ARCH001` example. |

## Dependency graphs

| File | Capture |
|---|---|
| `graph-no-code.png` | Dependency graph without code evidence. |
| `graph-with-code.png` | Dependency graph with code evidence and observed violations. |

Keep captures at a readable desktop resolution, use the Visual Studio dark theme, and crop them tightly enough that the relevant UI remains legible in the README.
