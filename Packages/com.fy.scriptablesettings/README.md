# Fy Scriptable Settings

A way to store your game's settings as `ScriptableObject` assets and read them from anywhere
by type. When you write a class that inherits from `ScriptableSettings`, it registers itself
in a global registry on load, and you can ask for it back in your code.

All settings live in a scriptable settings registry, which you can reach from anywhere in
your code. You can also inspect the registry and configure your settings in the custom editor
window that comes with the package.

## Usage

Make a class that derives from `ScriptableSettings` and put your fields on it:

```csharp
using Fy.ScriptableSettings;
using UnityEngine;

public sealed class AudioSettings : ScriptableSettings
{
    [SerializeField] private float _masterVolume = 1f;

    public float MasterVolume => _masterVolume;
}
```

Then read it from your code:

```csharp
if (ScriptableSettingsRegistry.TryGet(out AudioSettings audio))
{
    AudioListener.volume = audio.MasterVolume;
}
```

`TryGet` looks the type up and returns `false` if it isn't there. If you'd rather get a log
when something is missing, use `Get` instead:

```csharp
AudioSettings audio = ScriptableSettingsRegistry.Get<AudioSettings>();
```

`Get` returns the instance, or `null` plus an error in the console when the type isn't
registered. Neither call creates or loads assets behind your back, so the editor behaves the
same as a real build: a type is only available while its asset is loaded and preloaded.

The one step before any of this works is creating the asset, which you do from the window
below.

## The editor window

Open **Window > Fy > Scriptable Settings**. The left side lists every settings class in your
project. The right side shows the selected one: if its asset doesn't exist yet you get a
**Create** button, and once it does you get its fields to edit plus a small badge that reads
**In Build** or **Editor Only** so you can see at a glance whether it ships at runtime.

## Editor-only settings

By default every settings type is preloaded into your builds, so it resolves at runtime. When
a settings is meant only for editor tooling and should never ship in the player, mark the class
with `[EditorOnlySettings]`:

```csharp
using Fy.ScriptableSettings;

[EditorOnlySettings]
public sealed class BuildPipelineSettings : ScriptableSettings
{
}
```

The asset is then kept out of the build's preloaded assets, and the window shows it as
**Editor Only**. It's a property of the type, not the asset, so there's no per-asset toggle to
forget.

## Drawers

By default every settings
type uses the standard inspector layout, so you get an editable field for each serialized
member without writing any editor code.

When you want something more than a list of fields, write your own drawer. Derive from
`SettingsDrawerBase<T>` for your settings type and return the UI from `CreateBody`.

```csharp
using Fy.ScriptableSettings.Editor;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class AudioSettingsDrawer : SettingsDrawerBase<AudioSettings>
{
    public override VisualElement CreateBody(SerializedObject serializedObject)
    {
        VisualElement root = new VisualElement();

        SerializedProperty volume = serializedObject.FindProperty("_masterVolume");
        PropertyField volumeField = new PropertyField(volume, "Master Volume");
        volumeField.Bind(serializedObject);
        root.Add(volumeField);

        return root;
    }
}
```

You need to put the drawer class in an editor assembly in order to it work.
