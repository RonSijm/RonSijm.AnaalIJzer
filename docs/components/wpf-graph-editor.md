## WPF graph editor component

The WPF graph editor is the reusable visual editor behind the standalone graph editor harness and the Visual Studio dependency-graph tool window. I keep it outside the Visual Studio project because the graph is useful without Visual Studio too, and debugging WPF inside a VSIX every time would be an unnecessarily specific hobby.

| Project | Purpose |
|---|---|
| `src/Main/RonSijm.AnaalIJzer.Graphing` | Shared graph view models and layout grouping. |
| `src/Main/RonSijm.AnaalIJzer.Graphing.Wpf` | WPF/Nodify controls for viewing and editing architecture graphs. |
| `src/Tools/RonSijm.AnaalIJzer.GraphEditor.Standalone` | Small executable harness for testing the WPF component outside Visual Studio. |
| `src/Extensions/RonSijm.AnaalIJzer.VisualStudio` | Hosts the same WPF component inside the Visual Studio companion extension. |

The central controls are `ArchitectureGraphEditorControl` and `ArchitectureGraphCanvas`. Given an `ArchitectureGraphSnapshot`, they:

- render connected layer graphs from left to right;
- keep wildcard and global rules separate from the concrete graphs;
- preserve the user's layout in graph-editor user settings;
- add a separate read-only topology graph when a solution contains `<SolutionTopology>`.
  - That graph shows configured modules beside observed direct project references.
  - It is for investigation; topology edits still belong in the authoritative `Architecture.anl` file.

The editor is source-aware. It can edit XML settings files and inline `AssemblyMetadata("AnaalIJzerSettings", ...)` settings, then reload through the same configuration-reading path used by the analyzer tooling. The graph supports:

- dragging and resizing nested layer groups;
- collapsing graph groups;
- moving individual nodes without losing positions on refresh;
- creating root and child layers from context menus;
- drawing new dependencies from output connectors to input connectors;
- removing layers and dependencies;
- editing allowed/blocked dependency kind, site filters, descriptions and descendant cascading;
- editing layer matchers, scoped type policies, includes and root settings from the inspector;
- exporting the currently rendered graph surface to a PNG image.

When the standalone harness is opened from a `.csproj`, `.sln`, or `.slnx`, the graph exposes `Configuration fixes` in both the root inspector and the selected layer or connection inspector. That panel uses the same shared configuration-fix catalog as the Roslyn light bulbs and `arse fixes`, shows preview diffs, and can apply one proposal and immediately reload the diagram.

Right-clicking a layer or connection also offers a direct `Show configuration fixes` entry point. The resulting inspector view filters the loaded proposal list to the selected layer or dependency pair.

The component itself is not a Roslyn analyzer; dragging a box changes configuration, not code. The responsibilities are split like this:

- `RonSijm.AnaalIJzer.ConfigurationEditing` edits the configuration model.
- Visual Studio builds graph snapshots from the active Roslyn workspace.
- The standalone harness can open `Architecture.anl`, a project, a solution, or a legacy `.xml` input.
  - Project and solution inputs use the shared `MSBuildWorkspace` host.
  - The first configured project supplies the editable settings source.
  - Solution-wide code evidence is overlaid on the same diagram.

The `Export PNG` button is part of the shared WPF control, so it is available in both the standalone graph editor and the Visual Studio dependency-graph tool window. Tests can also call `ArchitectureGraphEditorControl.ExportGraphsAsPng(...)` directly for quick render smoke checks.

To regenerate a graph image for every example project, run:

```cmd
build\Scripts\GraphEditor\export-example-graph-images.cmd
```

By default, the script writes flat PNG artifacts to `build\Artifacts\ExampleGraphImages` and copies each image next to its example project as `<ExampleProjectName>-Graph.png`. Intentionally invalid diagnostic examples get a placeholder image instead of stopping the whole export run; one deliberately broken example should not take the rest of the catalog down with it.

Use `-Placement` to choose where the generated images go:

```cmd
build\Scripts\GraphEditor\export-example-graph-images.cmd -Placement Flat -OutputDirectory build\Artifacts\ExampleGraphImages
build\Scripts\GraphEditor\export-example-graph-images.cmd -Placement PreserveStructure -OutputDirectory build\Artifacts\ExampleGraphImages
build\Scripts\GraphEditor\export-example-graph-images.cmd -Placement SideBySide
build\Scripts\GraphEditor\export-example-graph-images.cmd -Placement All
```

The placement modes are:

- `Flat`: one export folder containing files such as `Example.IncludeSettings-Graph.png`.
- `PreserveStructure`: one export folder that keeps the `Examples` directory structure.
- `SideBySide`: each image is written next to its example project.
- `FlatAndSideBySide`: the default.
- `All`: writes all three shapes.

Build the standalone harness locally from the repository root:

```cmd
build\Scripts\GraphEditor\build-graph-editor-standalone.bat
```

The script writes the runnable output to `build\Artifacts\GraphEditor.Standalone`. You can also run the project output directly:

```cmd
src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\Release\net10.0-windows\RonSijm.AnaalIJzer.GraphEditor.Standalone.exe path\to\Architecture.anl
src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\Release\net10.0-windows\RonSijm.AnaalIJzer.GraphEditor.Standalone.exe path\to\MySolution.slnx
```

Use `Tools > Associate .anl files` in the standalone editor to make Windows open `.anl` files with the graph editor. The same operation is available from the executable:

```cmd
RonSijm.AnaalIJzer.GraphEditor.Standalone.exe --associate-anl
RonSijm.AnaalIJzer.GraphEditor.Standalone.exe --unassociate-anl
```

The GitHub `build_main.yml` workflow:

- builds the Windows-only editor;
- uploads `build\Artifacts\GraphEditor.Standalone` as a workflow artifact;
- publishes `AnaalIJzer-GraphEditor-Standalone-<version>.zip` as a release asset;
- replaces an existing `graph-editor-v<version>` release and tag before publishing the same version again.

The standalone graph editor is not shipped as a `dotnet tool install` package. The .NET SDK does not support `PackAsTool` for WPF or WindowsDesktop projects, so the packaging decision was made for us: Arse remains the command-line .NET tool while the graph editor is distributed as a Windows executable artifact and hosted inside the Visual Studio extension.

`RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests` covers:

- persistence from visual and inline-settings edits;
- context menus and connector-created dependencies;
- layout preservation and group collapse;
- theme behavior;
- configuration-fix previews, application, and selection-scoped filtering.
