## WPF graph editor component

The WPF graph editor is the reusable visual editor behind the standalone graph editor harness and the Visual Studio dependency-graph tool window.

| Project | Purpose |
|---|---|
| `src/Main/RonSijm.AnaalIJzer.Graphing` | Shared graph view models and layout grouping. |
| `src/Main/RonSijm.AnaalIJzer.Graphing.Wpf` | WPF/Nodify controls for viewing and editing architecture graphs. |
| `src/Tools/RonSijm.AnaalIJzer.GraphEditor.Standalone` | Small executable harness for testing the WPF component outside Visual Studio. |
| `src/Extensions/RonSijm.AnaalIJzer.VisualStudio` | Hosts the same WPF component inside the Visual Studio companion extension. |

The central controls are `ArchitectureGraphEditorControl` and `ArchitectureGraphCanvas`. They consume an `ArchitectureGraphSnapshot`, render connected layer graphs left-to-right, show wildcard/global rules separately, and preserve user layout through graph editor user settings.

The editor is source-aware. It can edit XML settings files and inline `AssemblyMetadata("AnaalIJzerSettings", ...)` settings, then reload through the same configuration-reading path used by the analyzer tooling. The graph supports:

- dragging and resizing nested layer groups;
- collapsing graph groups;
- moving individual nodes without losing positions on refresh;
- creating root and child layers from context menus;
- drawing new dependencies from output connectors to input connectors;
- removing layers and dependencies;
- editing allowed/blocked dependency kind, site filters, descriptions and descendant cascading;
- editing layer matchers, scoped type policies, includes and root settings from the inspector.
- exporting the currently rendered graph surface to a PNG image.

The component itself is not a Roslyn analyzer. It edits the configuration model through `RonSijm.AnaalIJzer.ConfigurationEditing`, and hosts decide where snapshots come from. Visual Studio builds snapshots from the active Roslyn workspace. The standalone harness can open `Architecture.anl`, `.xml`, `.csproj`, `.sln`, and `.slnx` inputs. Project and solution inputs use the shared MSBuildWorkspace tooling host, choose the first project with an AnaalIJzer configuration as the editable settings source, and overlay solution-wide code evidence on the diagram.

The `Export PNG` button is part of the shared WPF control, so it is available in both the standalone graph editor and the Visual Studio dependency-graph tool window. Tests can also call `ArchitectureGraphEditorControl.ExportGraphsAsPng(...)` directly for quick render smoke checks.

To regenerate a graph image for every example project, run:

```cmd
build\Scripts\export-example-graph-images.cmd
```

The script builds the standalone graph editor, writes flat PNG artifacts to `build\Artifacts\ExampleGraphImages`, and copies each image next to its example project as `<ExampleProjectName>-Graph.png`. Intentionally invalid diagnostic examples get a placeholder image instead of stopping the whole export run.

Build the standalone harness locally from the repository root:

```cmd
build\Scripts\build-graph-editor-standalone.bat
```

The script writes the runnable output to `build\Artifacts\GraphEditor.Standalone`. You can also run the project output directly:

```cmd
src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\Release\net10.0-windows\RonSijm.AnaalIJzer.GraphEditor.Standalone.exe path\to\Architecture.anl
src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\Release\net10.0-windows\RonSijm.AnaalIJzer.GraphEditor.Standalone.exe path\to\MySolution.slnx
```

The GitHub `build_main.yml` workflow also builds this Windows-only editor and uploads `build\Artifacts\GraphEditor.Standalone` as a workflow artifact.

The standalone graph editor is not shipped as a `dotnet tool install` package. The .NET SDK does not support `PackAsTool` for WPF or WindowsDesktop projects, so Arse remains the command-line dotnet tool while the graph editor is distributed as a Windows executable artifact and hosted inside the Visual Studio extension.

The WPF behavior is covered by `RonSijm.AnaalIJzer.Graphing.Wpf.Tests`, including persistence from visual edits, inline-settings edits, context menus, connector-created dependencies, layout preservation, group collapse and theme behavior.
