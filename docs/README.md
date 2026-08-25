# Documentation

The main repository README is generated from these files. Edit the topic docs first, then run:

```powershell
docs\build-readme.ps1
```

The GitHub wiki is generated from the same source notes by `.github/workflows/publish-wiki.yml` on pushes to `main`. To let that workflow publish successfully, enable the repository wiki, create the first wiki page once, and add a `WIKI_PUSH_TOKEN` repository secret with wiki write access.

This folder intentionally contains current product and tooling documentation only. Historical refactor plans and progress notes are not kept here.

## Main Topics

- [Introduction](introduction.md)
- [Setup](setup.md)
- [Configuration mental model](configuration/mental-model.md)
- [Configuration reference](configuration/index.md)
- [Diagnostics](diagnostics/index.md)
- [Arse TUI](tools/arse.md)
- [WPF graph editor component](components/wpf-graph-editor.md)
- [Visual Studio companion extension](components/visual-studio-addon.md)
- [Violation report](violation-report.md)
- [Architecture health](architecture-health.md)
- [Architecture documentation](architecture-documentation.md)

## Supporting Docs

- [Visual Studio companion manual acceptance checklist](visual-studio-companion-manual-acceptance.md)
