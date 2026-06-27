using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Fallback drawer used when a settings type has no custom <see cref="SettingsDrawerBase{T}"/>. It renders a default
    /// field for every serialized property except the built-in script reference, which the hub window already shows
    /// in its References section.
    /// </summary>
    public sealed class DefaultSettingsDrawer : ISettingsDrawer
    {
        private const string ScriptField = "m_Script";
        private const string PreloadField = "_preload";

        /// <inheritdoc/>
        public VisualElement CreateBody(SerializedObject serializedObject)
        {
            VisualElement root = new VisualElement();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == ScriptField || iterator.propertyPath == PreloadField)
                {
                    continue;
                }

                root.Add(new PropertyField(iterator.Copy()));
            }

            return root;
        }
    }
}
