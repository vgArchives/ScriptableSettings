# Fy Scriptable Settings

A global, type-keyed place to access `ScriptableObject`-based settings data. Each settings
asset registers itself to a central hub on load, saves as a normal project asset, preloads
into builds, and can supply its own editor UI drawer (with a standardized default).

Everything is edited through one custom hub window: **Window/Fy/Scriptable Settings**.

## Usage

Derive a settings type from `ScriptableSettings`:

```csharp
using Fy.ScriptableSettings;

public sealed class AudioSettings : ScriptableSettings
{
    [SerializeField] private float _masterVolume = 1f;

    public float MasterVolume => _masterVolume;
}
```

Read it anywhere at runtime:

```csharp
if (ScriptableSettingsRegistry.TryGet(out AudioSettings audio))
{
    AudioListener.volume = audio.MasterVolume;
}
```

Open the hub window, select your type, and press **Create** to make its asset (under
`Assets/Settings/`). Toggle **Preload** so the asset is available at runtime.

`TryGet` is a pure lookup (returns `false` when the type is not registered).
`ScriptableSettingsRegistry.Get<T>()` does the same but logs an error and returns `null`
when missing — neither one loads or creates assets, so the editor behaves like a build.

## Layout

- `Fy.ScriptableSettings` — runtime assembly: base class + static registry.
- `Fy.ScriptableSettings.Editor` — editor assembly: resolver, drawers, hub window, preload sync.
