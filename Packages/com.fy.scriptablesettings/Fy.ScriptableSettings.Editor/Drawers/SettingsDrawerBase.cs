using Fy.ScriptableSettings;
using UnityEditor;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Base class for a custom settings drawer. Subclass it for a concrete <typeparamref name="T"/> to provide a
    /// bespoke editor UI; the hub window discovers the subclass automatically and maps it to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The settings type this drawer renders.</typeparam>
    public abstract class SettingsDrawerBase<T> : ISettingsDrawer where T : ScriptableSettings
    {
        /// <inheritdoc/>
        public abstract VisualElement CreateBody(SerializedObject serializedObject);
    }
}
