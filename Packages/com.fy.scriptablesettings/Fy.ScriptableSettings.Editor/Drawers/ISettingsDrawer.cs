using UnityEditor;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Builds the editor UI body for a single settings asset inside the hub window.
    /// </summary>
    public interface ISettingsDrawer
    {
        /// <summary>
        /// Creates the visual body for the given settings asset.
        /// </summary>
        /// <param name="serializedObject">The serialized representation of the settings asset to edit.</param>
        VisualElement CreateBody(SerializedObject serializedObject);
    }
}
