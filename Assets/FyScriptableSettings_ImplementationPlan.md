# Fy Scriptable Settings — Implementation Plan

A from-scratch reimplementation of Coimbra's ScriptableSettings pattern, adapted to a
single, self-owned hub window with per-class UI drawers. Architecture mirrors the
`Fy.Services` package; code follows `A:\UnityProjects\Guidelines\StyleGuide.cs` and
`ProgGuidelines.md` (these override Coimbra style where they differ).

---

## 0. When in doubt — reference order

> **Design / architecture doubt:** consult the **Coimbra ScriptableSettings** source first.
> It is the canonical reference for *how this pattern is structured* (registration lifecycle,
> preload, scopes, save/load, provider/window split). Adapt — do not blindly copy — to our
> single-scope, own-hub decisions in this doc.
>
> **Code / style doubt:** consult the **Guidelines + Style Guide** (they win over Coimbra's
> style every time). Coimbra shows *what* to do architecturally; the guidelines decide *how*
> the code is written (naming, braces, ordering, access modifiers).

**Reference paths:**

| Purpose | Path |
|---|---|
| Coimbra ScriptableSettings — core | `A:\UnityProjects\Kaardik\Packages\com.coimbrastudios.core@11.0.5\Coimbra\ScriptableSettings.cs` |
| Coimbra ScriptableSettings — type enum / attributes | `A:\UnityProjects\Kaardik\Packages\com.coimbrastudios.core@11.0.5\Coimbra\` (`ScriptableSettingsType.cs`, `ProjectSettingsAttribute.cs`, `PreferencesAttribute.cs`) |
| Coimbra ScriptableSettings — editor (save/load, window, provider, preprocessor) | `A:\UnityProjects\Kaardik\Packages\com.coimbrastudios.core@11.0.5\Coimbra.Editor\` (`ScriptableSettingsProvider.cs`, `ScriptableSettingsWindow.cs`, `ScriptableSettingsBuildPreprocessor.cs`, `Utilities\ScriptableSettingsUtility.cs`) |
| Coimbra docs | `A:\UnityProjects\Kaardik\Packages\com.coimbrastudios.core@11.0.5\Documentation~\ScriptableSettings.md` |
| Architecture precedent (own package) — Fy.Services | `A:\UnityProjects\ServiceLocator\Packages\com.fy.services\` |
| Programming guidelines | `A:\UnityProjects\Guidelines\ProgGuidelines.md` |
| Style guide | `A:\UnityProjects\Guidelines\StyleGuide.cs` |

---

## 1. Goal

A global, type-keyed place to access ScriptableObject-based settings data, where each
settings asset:

- **registers itself** to a central hub on load (no manual wiring),
- **saves** as a normal project asset,
- **preloads** into builds so it is available at runtime,
- can supply **its own UI drawer** (with a standardized default when it doesn't),

all edited through **one custom hub window** (`Window/Fy/Scriptable Settings`) — replacing
Unity's Project Settings / Preferences surfaces entirely.

---

## 2. The Five Pillars (mental model)

1. **Base class** `ScriptableSettings : ScriptableObject` — the data container + self-registration.
2. **Static registry** `ScriptableSettingsRegistry` — `Dictionary<Type, ScriptableSettings>` + typed getters.
3. **Drawer system** `SettingsDrawer<T>` + `DefaultSettingsDrawer` — per-class editor UI, `TypeCache`-discovered.
4. **Persistence** — plain `.asset` files in `Assets/Settings/`, auto-saved.
5. **Preload** — `PlayerSettings` Preloaded Assets list, kept in sync by editor tooling.

Runtime assembly owns pillars 1, 2 and the runtime half of 5. Editor assembly owns pillars
3, 4 (save), and the sync half of 5. The two halves connect through a single delegate
(`FindFallback`) so the runtime assembly never references `UnityEditor`.

---

## 3. Package Layout

Standalone Unity project (Unity 6000.0). The system lives as an embedded package so it is a
folder-copy away from being reused in Kaardik.

```
Packages/com.fy.scriptablesettings/
├── package.json
├── README.md
├── Fy.ScriptableSettings/                              (RUNTIME asmdef — autoReferenced)
│   ├── Fy.ScriptableSettings.asmdef
│   ├── AssemblyInfo.cs                        InternalsVisibleTo Editor + both test asmdefs
│   ├── Core/
│   │   ├── ScriptableSettings.cs              base class (self-registration + _preload)
│   │   └── ScriptableSettingsRegistry.cs      static hub (Map, TryGet, Get, FindFallback)
│   └── Utility/
│       └── (helpers if needed)
├── Fy.ScriptableSettings.Editor/                       (EDITOR asmdef — Editor platform only)
│   ├── Fy.ScriptableSettings.Editor.asmdef             references Fy.ScriptableSettings
│   ├── Drawers/
│   │   ├── ISettingsDrawer.cs
│   │   ├── SettingsDrawer.cs                  abstract SettingsDrawer<T>
│   │   ├── DefaultSettingsDrawer.cs           auto-fields via InspectorElement
│   │   └── SettingsDrawerRegistry.cs          TypeCache discovery: settings type -> drawer
│   ├── ScriptableSettingsEditorResolver.cs    assigns FindFallback (LoadOrCreate on disk)
│   ├── ScriptableSettingsWindow.cs            the hub window (TwoPaneSplitView)
│   ├── Preload/
│   │   ├── ScriptableSettingsPreloadSync.cs   add/remove/reconcile PlayerSettings list
│   │   └── ScriptableSettingsBuildPreprocessor.cs  authoritative reconcile at build
│   └── Styling/
│       └── SettingsWindowStyles.cs            theme colors + helpers (ported from ServiceLocator)
├── Fy.ScriptableSettings.RuntimeTests/
│   ├── Fy.ScriptableSettings.RuntimeTests.asmdef
│   └── (registry / access / registration tests)
├── Fy.ScriptableSettings.EditorTests/
│   ├── Fy.ScriptableSettings.EditorTests.asmdef
│   └── (preload reconcile / drawer discovery / default drawer tests)
└── Samples~/
    └── Examples/
        ├── Fy.ScriptableSettings.Examples.asmdef
        ├── DefaultDrawer/   simple settings using the auto-generated drawer
        ├── WeaknessTable/   settings + custom SettingsDrawer<T> grid (headline demo)
        └── Runtime/         MonoBehaviour reading a setting via TryGet
```

**Naming:** package `com.fy.scriptablesettings`, displayName "Fy Scriptable Settings", namespaces
`Fy.ScriptableSettings`, `Fy.ScriptableSettings.Editor`, `Fy.ScriptableSettings.RuntimeTests`, `Fy.ScriptableSettings.EditorTests`.

---

## 4. Implementation Phases

Each phase is independently testable. Build in order; do not start a phase before the
previous one compiles and its check passes.

### Phase 0 — Project & package scaffolding

**Files:** `package.json`, both runtime/editor `.asmdef`, `AssemblyInfo.cs`, `README.md`.

Steps:
1. Create the standalone Unity 6000.0 project.
2. Create `Packages/com.fy.scriptablesettings/` and `package.json` (copy the `Fy.Services` shape:
   name, displayName, version `0.1.0`, `unity` `6000.0`, author, MIT, `samples` entry for
   `Samples~/Examples`).
3. Create the **runtime** asmdef `Fy.ScriptableSettings` (`rootNamespace` `Fy.ScriptableSettings`,
   `autoReferenced: true`, no extra references).
4. Create the **editor** asmdef `Fy.ScriptableSettings.Editor` (`includePlatforms: ["Editor"]`,
   references `Fy.ScriptableSettings`).
5. Add `AssemblyInfo.cs` in the runtime asmdef with `InternalsVisibleTo` for
   `Fy.ScriptableSettings.Editor`, `Fy.ScriptableSettings.RuntimeTests`, `Fy.ScriptableSettings.EditorTests`
   (so editor/tests can use `internal` registry members like `Enumerate`/`Set`).

**Check:** project compiles with two empty assemblies.

---

### Phase 1 — Runtime core (registry + base class)

**Files:** `ScriptableSettingsRegistry.cs`, `ScriptableSettings.cs`.

`ScriptableSettingsRegistry` (static class):
- `private static readonly Dictionary<Type, ScriptableSettings> Map = new();`
- `public static Func<Type, ScriptableSettings> FindFallback;` (editor assigns this).
- `public static bool TryGet<T>(out T result) where T : ScriptableSettings` — pure lookup;
  no fallback, no side effects.
- `public static T Get<T>() where T : ScriptableSettings` — lookup; if missing, try
  `FindFallback`; if still missing, **`Debug.LogError`** and return `null`.
- `internal static void Set(Type type, ScriptableSettings value)` — register; if a *different*
  instance is already mapped, `Debug.LogWarning` (soft singleton) and keep first.
- `internal static void SetOrOverwrite(Type type, ScriptableSettings value)` — force set / remove.
- `internal static IEnumerable<...Snapshot> Enumerate()` — for the hub window.
- `internal static void Reset()` — clears Map (used by editor play-mode reset).

`ScriptableSettings` (abstract : ScriptableObject):
- `[SerializeField] private bool _preload = true;` + `public bool Preload => _preload;`
- non-virtual `private void OnEnable()` → `OnLoaded();`
- non-virtual `private void OnDisable()` → if current instance, `SetOrOverwrite(type, null)`, then `OnUnload();`
- `protected virtual void OnLoaded()` → `Registry.Set(GetType(), this);`
- `protected virtual void OnUnload()` → empty hook.

Style notes: braces on new line, `_camelCase` field, explicit access modifiers, XML
`<summary>` on public members, no redundant initializers except the intentional `= true`.

**Check (RuntimeTests later):** creating an instance registers it; destroying unregisters;
`TryGet` finds it; a second instance warns and does not replace.

---

### Phase 2 — Editor resolution (FindFallback + auto-create)

**Files:** `ScriptableSettingsEditorResolver.cs`.

- `[InitializeOnLoadMethod]` assigns `ScriptableSettingsRegistry.FindFallback = LoadOrCreate;`
- `LoadOrCreate(Type)`:
  1. `AssetDatabase.FindAssets($"t:{type.Name}")` → load first match; if multiple, warn
     (lists duplicate paths) and use the first.
  2. If none, ensure `Assets/Settings/` exists, `CreateInstance(type)`, `CreateAsset` at
     `Assets/Settings/{type.Name}.asset`, `SaveAssets`.
  3. Return the instance (its `OnEnable` registers it).
- Expose the default-folder path as a single `const string DefaultFolder = "Assets/Settings";`.

This gives "just works in the editor" while the runtime stays clean (delegate-injected).

**Check:** in edit-mode, `Get<T>()` for a never-created type produces an asset under
`Assets/Settings/` and resolves.

---

### Phase 3 — Preload sync

**Files:** `ScriptableSettingsPreloadSync.cs`, `ScriptableSettingsBuildPreprocessor.cs`.

`ScriptableSettingsPreloadSync` (static helpers):
- `SetPreload(ScriptableSettings asset, bool value)` — sets `_preload` (via SerializedObject),
  saves, then immediately updates `PlayerSettings.SetPreloadedAssets(...)` (add/remove).
- `Reconcile()` — authoritative pass:
  1. Get current preloaded list; **prune nulls + de-duplicate**.
  2. For every settings asset found via `FindAssets("t:ScriptableSettings")`: add if `_preload`,
     remove if not.
  3. Write back with `SetPreloadedAssets`.

`ScriptableSettingsBuildPreprocessor : IPreprocessBuildWithReport` → `OnPreprocessBuild` calls
`Reconcile()` so the shipped list is always correct regardless of editor drift.

**Check (EditorTests):** toggling preload updates the list; reconcile prunes a forced null and
a forced duplicate; an excluded asset is removed.

---

### Phase 4 — Drawer system

**Files:** `ISettingsDrawer.cs`, `SettingsDrawer.cs`, `DefaultSettingsDrawer.cs`,
`SettingsDrawerRegistry.cs`.

- `ISettingsDrawer` → `VisualElement CreateBody(SerializedObject serializedObject);`
- `public abstract class SettingsDrawer<T> : ISettingsDrawer where T : ScriptableSettings` —
  base for custom drawers; subclass implements `CreateBody`.
- `DefaultSettingsDrawer : ISettingsDrawer` — fallback; returns an `InspectorElement` bound to
  the `SerializedObject` (auto-generated fields; respects any standard `[CustomEditor]` too).
- `SettingsDrawerRegistry` — `TypeCache.GetTypesDerivedFrom(typeof(SettingsDrawer<>))`, map each
  drawer's generic argument (settings type) → drawer instance. `GetDrawer(Type settingsType)`
  returns the custom drawer or a `DefaultSettingsDrawer`.

**Check (EditorTests):** a type with a registered `SettingsDrawer<T>` returns it; a type without
returns `DefaultSettingsDrawer`.

---

### Phase 5 — Hub window

**Files:** `ScriptableSettingsWindow.cs`, `SettingsWindowStyles.cs`.

- `[MenuItem("Window/Fy/Scriptable Settings")]` → `EditorWindow`.
- `CreateGUI`: `TwoPaneSplitView`.
  - **Left:** flat `ListView` of all discovered settings types
    (`TypeCache.GetTypesDerivedFrom<ScriptableSettings>()`, non-abstract), each row = default SO
    icon + type display name. Sorted alphabetically.
  - **Right (detail):** standardized header for the selected type —
    type name, namespace, **Preload toggle** + **in-build / excluded indicator**, and either
    the drawer body OR a **Create** button when no asset exists yet.
- On selection: resolve/load the asset via the editor resolver; build the drawer via
  `SettingsDrawerRegistry.GetDrawer(...)`; bind it to the asset's `SerializedObject`.
- **Auto-save:** on any bound change → `ApplyModifiedProperties` → `SetDirty` →
  `SaveAssetIfDirty`. Preload toggle routes through `PreloadSync.SetPreload`.
- Port theme colors / `SetBorderRadius` / sub-section helpers from `ServiceLocatorWindow`
  into `SettingsWindowStyles` so both windows look like one family. (No assembly grouping,
  no cards-per-assembly — that was ServiceLocator-specific.)

**Check:** select a type → drawer + data appear; edit a field → asset file updates on disk;
toggle preload → indicator + PlayerSettings list update; uncreated type shows Create → creates asset.

---

### Phase 6 — Editor play-mode reset

In `ScriptableSettingsRegistry` (guarded by `#if UNITY_EDITOR`) or the resolver: subscribe to
`EditorApplication.playModeStateChanged`; on `ExitingPlayMode` call `Registry.Reset()` so stale
runtime instances don't linger between sessions (mirrors `ServiceLocator.CleanupOnPlayModeExit`).

**Check:** enter/exit play mode repeatedly; no duplicate-registration warnings accumulate.

---

### Phase 7 — Tests

- `Fy.ScriptableSettings.RuntimeTests`: registration on enable/disable, `TryGet` vs `Get`, error-log on
  missing (`LogAssert.Expect`), soft-singleton duplicate warning, `FindFallback` invocation.
- `Fy.ScriptableSettings.EditorTests`: preload reconcile (add/remove/prune/dedupe), drawer discovery,
  default-drawer fallback, auto-create folder behavior.

---

### Phase 8 — Samples

`Samples~/Examples` (own asmdef referencing runtime; editor sample bits guarded/ in an editor
folder):
1. **DefaultDrawer** — a simple settings type (a few fields) using the auto drawer.
2. **WeaknessTable** — a settings type holding a type-effectiveness grid + a custom
   `SettingsDrawer<WeaknessTableSettings>` rendering an editable grid (the headline feature).
3. **Runtime** — a `MonoBehaviour` that reads a setting via `ScriptableSettingsRegistry.TryGet`
   in `Start` and logs/uses it (end-to-end proof).

---

### Phase 9 — IL2CPP stripping validation (milestone)

Produce a player build with **IL2CPP + Managed Stripping Level = High**. Run it and confirm
preloaded settings still register and resolve at runtime (e.g. the Runtime sample logs correct
data).
- If it works → no link.xml needed (preloaded asset references anchor the types). Done.
- If anything is stripped → add an `IUnityLinkerProcessor` generator (ported from
  `ServicesLinkXmlGenerator`) preserving all `ScriptableSettings` subclasses, and re-test.

---

## 5. Runtime / Editor boundary (critical)

| Concern | Assembly | Why |
|---|---|---|
| `ScriptableSettings`, `ScriptableSettingsRegistry`, `_preload`, `OnEnable` registration | Runtime | needed in builds |
| `FindFallback` *delegate field* | Runtime | injected, no `UnityEditor` dependency |
| `FindFallback` *implementation*, auto-create, all drawers, hub window, preload sync, build preprocessor | Editor | use `UnityEditor` / `SerializedObject` / `AssetDatabase` |

`VisualElement` is in `UnityEngine.UIElements` (runtime-safe), but drawers use
`SerializedObject` (editor-only), so all drawers live in the Editor assembly.

---

## 6. Style conformance checklist (applies to every file)

- `_camelCase` private fields; `PascalCase` public; `camelCase` locals/params.
- Braces always on a new line; no single-line bodies; keep braces in nested blocks.
- Explicit access modifiers everywhere; one type per file; no `#region`.
- Member order: Events → Fields → Properties → Unity methods → public → internal → private.
- XML `<summary>` on public members where it aids Intellisense; comments explain *why*, not *what*.
- ~120 col width; drop redundant initializers (except intentional `_preload = true`).
- Booleans read as questions (`isPreloaded`, `hasAsset`).

---

## 7. Deferred / optional (do NOT build now — only on real pain)

- Editor-only scope (second scope) — add an attribute + one branch later if needed.
- Per-type custom icons in the list.
- `AssetPostprocessor`-based preload sync (only if build-time reconcile proves insufficient).
- link.xml generator (only if Phase 9 fails).
- Configurable settings root folder beyond the single `const`.
```
