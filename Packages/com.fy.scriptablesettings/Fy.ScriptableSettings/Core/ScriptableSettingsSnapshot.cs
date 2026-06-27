using System;

namespace Fy.ScriptableSettings
{
    /// <summary>
    /// Immutable view of a single registered <see cref="ScriptableSettings"/> entry, used by editor tooling.
    /// </summary>
    internal readonly struct ScriptableSettingsSnapshot
    {
        public readonly Type SettingsType;
        public readonly ScriptableSettings Value;

        public ScriptableSettingsSnapshot(Type settingsType, ScriptableSettings value)
        {
            SettingsType = settingsType;
            Value = value;
        }
    }
}
