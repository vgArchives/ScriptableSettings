using Fy.ScriptableSettings.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.EditorTests
{
    public sealed class CustomDrawnSettingsDrawer : SettingsDrawerBase<CustomDrawnSettings>
    {
        public override VisualElement CreateBody(SerializedObject serializedObject)
        {
            return new VisualElement();
        }
    }
}
