using Fy.ScriptableSettings.Editor;
using Fy.ScriptableSettings.Examples;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.Examples.Editor
{
    /// <summary>
    /// Custom drawer that renders <see cref="WeaknessTableSettings"/> as an editable attacker x defender grid.
    /// Demonstrates the headline feature: a per-class bespoke editor surface inside the hub window.
    /// </summary>
    public sealed class WeaknessTableSettingsDrawer : SettingsDrawerBase<WeaknessTableSettings>
    {
        private const float LabelCellWidth = 64f;
        private const float ValueCellWidth = 48f;

        public override VisualElement CreateBody(SerializedObject serializedObject)
        {
            var settings = (WeaknessTableSettings)serializedObject.targetObject;

            var root = new VisualElement();
            var grid = new VisualElement();

            var resetButton = new Button(() =>
            {
                settings.ResetToDefault();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
                serializedObject.Update();
                RebuildGrid(grid, serializedObject, settings);
            })
            {
                text = "Reset Grid To Default"
            };
            resetButton.style.alignSelf = Align.FlexStart;
            resetButton.style.marginBottom = 6;
            root.Add(resetButton);

            root.Add(grid);
            RebuildGrid(grid, serializedObject, settings);

            return root;
        }

        private static void RebuildGrid(VisualElement grid, SerializedObject serializedObject,
            WeaknessTableSettings settings)
        {
            grid.Clear();

            int count = settings.Count;

            if (count == 0)
            {
                grid.Add(new Label("No elements configured. Use Reset Grid To Default to populate."));

                return;
            }

            SerializedProperty multipliers = serializedObject.FindProperty("_multipliers");

            if (multipliers.arraySize != count * count)
            {
                grid.Add(new Label("Grid data is out of sync. Press Reset Grid To Default to rebuild it."));

                return;
            }

            VisualElement header = CreateRow();
            header.Add(CreateLabelCell(string.Empty));

            for (int defender = 0; defender < count; defender++)
            {
                header.Add(CreateLabelCell(settings.Elements[defender].ToString()));
            }

            grid.Add(header);

            for (int attacker = 0; attacker < count; attacker++)
            {
                VisualElement row = CreateRow();
                row.Add(CreateLabelCell(settings.Elements[attacker].ToString()));

                for (int defender = 0; defender < count; defender++)
                {
                    SerializedProperty cell = multipliers.GetArrayElementAtIndex((attacker * count) + defender);

                    var field = new FloatField();
                    field.style.width = ValueCellWidth;
                    field.BindProperty(cell);
                    row.Add(field);
                }

                grid.Add(row);
            }
        }

        private static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            return row;
        }

        private static Label CreateLabelCell(string text)
        {
            var label = new Label(text);
            label.style.width = LabelCellWidth;
            label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;

            return label;
        }
    }
}
