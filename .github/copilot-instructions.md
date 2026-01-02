## Purpose
Provide concise, actionable guidance for AI coding agents working in this Unity project so they can be immediately productive.

## Big picture
- Unity 3D project organized under `Assets/` with a custom simulation called the Mutatron.
- The `MutatronEngine` (see [Assets/Mindeleev/Scripts/MutatronEngine.cs](Assets/Mindeleev/Scripts/MutatronEngine.cs#L1)) is the central orchestrator for grid state, polytrons, and level config.
- `BindingManager` (see [Assets/Mindeleev/Scripts/BindingManager.cs](Assets/Mindeleev/Scripts/BindingManager.cs#L1-L12)) encapsulates polytron binding/unbinding and is an incremental refactor extracted from `MutatronEngine`.
- Data flow: `gridCellsMap` (in `MutatronEngine`) holds `HexCellData` (tile, sink, ring). Recipes are stored as strings on tiles and parsed via `PolyhedronRecipeParser` to decide binding, rebuilding, and grouping by operator sequences.

## Key components & where to look
- Orchestration: `Assets/Mindeleev/Scripts/MutatronEngine.cs` — engine lifecycle, level config, `polytrons`, `gridCellsMap`.
- Binding logic: `Assets/Mindeleev/Scripts/BindingManager.cs` — contains binding/unbinding, home/center rules, and reconciliation logic.
- Grid visuals & sinks: `Assets/Mindeleev/Scripts/GridManager.cs`.
- Recipe parsing & domain model: `PolyhedronRecipe*` files (search `PolyhedronRecipeParser` and `PolyhedronRecipe`).
- Assembly definitions: `Assets/Mindeleev/Scripts/ScriptsAssembly.asmdef` controls compilation boundaries; tests live under `Assets/Mindeleev/Tests` with `Tests.asmdef`.

## Project-specific conventions (concrete, not generic)
- `ring == 12` frequently denotes the external/home ring — code treats those sinks specially (see `BindPolytronToSink` logic in `BindingManager`).
- `reservedForGenetics` flag: polytrons with this flag are excluded from automated moves/rebuilds.
- `architronIdx` selects the Architron polytron that must be bound to the Mutatron center.
- Pre-bind rebuild pattern: when a polytron is being bound from home to the Mutatron, the code rebuilds the polytron using the tile recipe BEFORE changing the polytron's `boundSink` (to preserve “home” semantics).
- Operator-groups: recipes are grouped by `OperatorsSequence()` to reconcile sinks vs polytrons; look in `BindingManager.UnbindNonMatchingPolytrons()` for the reconciliation algorithm.

## Build / run / test notes (practical)
- Open the project in Unity Editor for normal development. Assembly definitions are used; avoid editing generated `.csproj` files directly.
- Run Editor/PlayMode tests with the Unity Test Runner. Example CLI to run editor-playmode tests (adjust Unity path and project path):

```bash
/Applications/Unity/Hub/Editor/<VERSION>/Unity \
  -batchmode -projectPath "/path/to/MindeleevSpatial" \
  -runEditorTests -testPlatform PlayMode -testResults "TestResults.xml" -quit
```

- Use the Unity Console for `Debug.Log` diagnostics; many modules rely on logs and warn/throw patterns.

## Integration & external dependencies
- The repo contains `Spatial` SDK folders and an export `Exports/spaces.unitypackage` — update integration carefully and prefer using the Unity Package Manager or the exported package.
- Shaders live under `Assets/Mindeleev/Shaders` and are used for visuals; editing these can affect rendering behavior in scenes.

## Common refactor signals
- Large `/* ... */` commented-out methods in `BindingManager` indicate ongoing refactor. Preserve semantics (binding order, pre-bind rebuild) when extracting logic.
- Many methods swallow parse exceptions around recipe parsing — preserve this cautious behavior unless tests are updated to assert parsing guarantees.

## Quick tasks examples for agents
- To change binding rules: edit logic in `BindingManager.BindPolytronToSink` (preserve pre-rebuild-before-assign order) and add unit tests under `Assets/Mindeleev/Tests`.
- To inspect grid state in runtime: instrument `MutatronEngine.gridCellsMap` and `polytrons` with detailed `Debug.Log` or temporary editor gizmos in `GridManager`.

## Where to run/verify changes
- Use Play mode in the Editor for integration verification; run Tests from `Assets/Mindeleev/Tests` for unit-level checks.
- If adding public API surfaces, update `ScriptsAssembly.asmdef` and ensure tests compile against assembly definitions.

## If uncertain, check these files first
- [Assets/Mindeleev/Scripts/BindingManager.cs](Assets/Mindeleev/Scripts/BindingManager.cs#L1-L12)
- [Assets/Mindeleev/Scripts/MutatronEngine.cs](Assets/Mindeleev/Scripts/MutatronEngine.cs#L1)
- [Assets/Mindeleev/Scripts/GridManager.cs](Assets/Mindeleev/Scripts/GridManager.cs#L1)
- [Assets/Mindeleev/Scripts/ScriptsAssembly.asmdef](Assets/Mindeleev/Scripts/ScriptsAssembly.asmdef)

---
If anything above is unclear or you want additional examples (e.g., focused on tests or the recipe parser), tell me which area to expand and I will iterate.
