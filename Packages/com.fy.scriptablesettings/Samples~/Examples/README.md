# Settings Examples

Import via the package's **Samples** tab in the Package Manager, then open
**Window/Fy/Scriptable Settings**.

## DefaultDrawer
`GameplaySettings` — a few plain fields (`startingLives`, `difficultyMultiplier`,
`tutorialEnabled`) rendered by the standardized default drawer. Select it in the hub
window and edit; the asset is created and saved automatically.

## WeaknessTable (headline)
`WeaknessTableSettings` holds an attacker x defender type-effectiveness grid.
`WeaknessTableSettingsDrawer` (editor-only) renders it as an editable table inside the
hub window. Press **Reset Grid To Default** to populate every `ElementType` with a
neutral 1x multiplier, then edit individual cells.

## EditorOnly
`LevelEditorSettings` is marked with `[EditorOnlySettings]`, so it's kept out of builds and
shows up under the hub window's **Editor Only** tab instead of **Runtime**.

## Runtime
`SettingsReader` is a `MonoBehaviour` that reads `GameplaySettings` via
`ScriptableSettingsRegistry.TryGet` in `Start` and logs it — proving preloaded settings
resolve at runtime with no manual wiring. Add it to a GameObject, make sure
`GameplaySettings` exists (it's preloaded by default), and press Play.
