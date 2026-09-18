# Anaaltomy Azure DevOps Extension Notes

Anaaltomy's interactive viewer can be shipped as an Azure DevOps web extension because the viewer is a static Blazor WebAssembly app. The extension should contribute a project-level hub and serve the published viewer assets from the VSIX package.

## Recommended Contribution

- Contribution type: `ms.vss-web.hub`
- Target: `ms.vss-code-web.code-hub-group` for Azure Repos, or `ms.vss-build-web.build-release-hub-group` if the viewer is meant to live near build artifacts.
- Hub properties: `name`, `uri`, optional `order`, optional `icon` or `iconName`.
- Viewer input model: start with manual upload of `anaalijzer.db`; later, use Azure DevOps REST APIs and `azure-devops-extension-sdk` to discover and download build artifacts.

## Marketplace Requirements

- Create a Visual Studio Marketplace publisher and use that publisher ID in `vss-extension.json`.
- Add `vss-extension.json` at the extension package root.
- Required manifest fields include `manifestVersion`, `id`, `version`, `name`, `publisher`, `categories`, and `targets`.
- Use target `Microsoft.VisualStudio.Services` for Azure DevOps Services extensions.
- Include a Marketplace `overview.md`.
- Include an extension icon at least 128x128 pixels.
- Use `tfx-cli` to create the VSIX package: `npx tfx-cli extension create`.
- Increment the manifest version for every update.
- Keep the VSIX below 50 MB; if the packaged Blazor output grows, trim dependencies and bundle only required static assets.
- The current viewer uses `sql.js` from a CDN for the browser SQLite runtime. Before publishing publicly, vendor `sql-wasm.js` and `sql-wasm.wasm` into the extension package so the extension works without external CDN dependencies.

## Minimal Manifest Shape

```json
{
  "manifestVersion": 1,
  "id": "anaaltomy",
  "version": "0.1.0",
  "name": "Anaaltomy",
  "publisher": "<publisher-id>",
  "description": "Interactive code statistics for Anaaltomy SQLite reports.",
  "targets": [
    {
      "id": "Microsoft.VisualStudio.Services"
    }
  ],
  "categories": [
    "Azure Repos"
  ],
  "icons": {
    "default": "images/icon-anaaltomy.png"
  },
  "content": {
    "details": {
      "path": "overview.md"
    }
  },
  "contributions": [
    {
      "id": "anaaltomy-hub",
      "type": "ms.vss-web.hub",
      "targets": [
        "ms.vss-code-web.code-hub-group"
      ],
      "properties": {
        "name": "Anaaltomy",
        "uri": "dist/index.html",
        "iconName": "AnalyticsView",
        "supportsMobile": true
      }
    }
  ],
  "files": [
    {
      "path": "dist",
      "addressable": true
    },
    {
      "path": "images",
      "addressable": true
    },
    {
      "path": "overview.md",
      "addressable": true
    }
  ]
}
```

## Sources Checked

- Microsoft Learn: [Azure DevOps extension manifest reference](https://learn.microsoft.com/en-us/azure/devops/extend/develop/manifest?view=azure-devops)
- Microsoft Learn: [Develop a web extension](https://learn.microsoft.com/en-us/azure/devops/extend/get-started/node?view=azure-devops)
- Microsoft Learn: [Add a hub](https://learn.microsoft.com/en-us/azure/devops/extend/develop/add-hub?view=azure-devops)
- Microsoft Learn: [Package and publish an integration](https://learn.microsoft.com/en-us/azure/devops/extend/publish/integration?view=azure-devops)
- Microsoft GitHub: [`azure-devops-extension-sdk`](https://github.com/microsoft/azure-devops-extension-sdk)
