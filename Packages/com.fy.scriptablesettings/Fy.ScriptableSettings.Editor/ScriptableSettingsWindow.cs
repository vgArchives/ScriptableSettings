using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fy.ScriptableSettings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Single hub window for every <see cref="ScriptableSettings"/> type. The left pane lists all discovered types;
    /// the right pane shows a standardized header, the preload toggle and build indicator, and either the type's
    /// drawer body or a Create button when no asset exists yet.
    /// </summary>
    public sealed class ScriptableSettingsWindow : EditorWindow
    {
        private readonly List<Type> _types = new();

        private ListView _list;
        private ScrollView _detail;
        private Texture _settingsIcon;

        [MenuItem("Window/Fy/Scriptable Settings")]
        public static void Open()
        {
            ScriptableSettingsWindow window = GetWindow<ScriptableSettingsWindow>();
            window.titleContent = new GUIContent("Scriptable Settings");
        }

        private void CreateGUI()
        {
            rootVisualElement.style.fontSize = 13;
            _settingsIcon = EditorGUIUtility.ObjectContent(null, typeof(ScriptableObject)).image;

            RefreshTypes();

            TwoPaneSplitView split = new TwoPaneSplitView(0, 260f, TwoPaneSplitViewOrientation.Horizontal);

            VisualElement leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;

            _list = new ListView
            {
                itemsSource = _types,
                fixedItemHeight = 22f,
                selectionType = SelectionType.Single,
                makeItem = MakeRow,
                bindItem = BindRow
            };
            _list.style.flexGrow = 1;
            _list.selectionChanged += HandleSelectionChanged;
            leftPane.Add(_list);
            leftPane.Add(BuildRootFolderFooter());
            split.Add(leftPane);

            _detail = new ScrollView();
            _detail.style.flexGrow = 1;
            split.Add(_detail);

            rootVisualElement.Add(split);

            if (_types.Count > 0)
            {
                _list.selectedIndex = 0;
            }
            else
            {
                ShowPlaceholder("No settings types found.");
            }
        }

        private void RefreshTypes()
        {
            _types.Clear();
            _types.AddRange(TypeCache.GetTypesDerivedFrom<ScriptableSettings>()
                .Where(type => !type.IsAbstract && !type.IsGenericType && !IsTestAssembly(type.Assembly))
                .OrderBy(type => type.Name));
        }

        /// <summary>
        /// Test-only settings types should not appear in the hub. An assembly is treated as a test assembly when it
        /// references the NUnit framework or the Unity test runner.
        /// </summary>
        private static bool IsTestAssembly(Assembly assembly)
        {
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == "nunit.framework" || reference.Name == "UnityEngine.TestRunner")
                {
                    return true;
                }
            }

            return false;
        }

        private VisualElement MakeRow()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 1;
            row.style.paddingLeft = SettingsWindowStyles.Space1;

            VisualElement marker = SettingsWindowStyles.CreateRightArrow(new Color(0.55f, 0.57f, 0.60f));
            marker.name = "marker";
            marker.style.marginRight = SettingsWindowStyles.Space1;
            marker.style.visibility = Visibility.Hidden;
            row.Add(marker);

            Image icon = new Image { name = "icon" };
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = SettingsWindowStyles.Space1;
            row.Add(icon);

            Label label = new Label { name = "label" };
            row.Add(label);

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            Type type = _types[index];
            element.Q<Image>("icon").image = ResolveRowIcon(type);
            element.Q<Label>("label").text = ObjectNames.NicifyVariableName(type.Name);
            element.style.backgroundColor =
                index % 2 == 0 ? SettingsWindowStyles.RowDarkColor : SettingsWindowStyles.RowLightColor;
            element.Q("marker").style.visibility =
                index == _list.selectedIndex ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Uses the created asset's own icon when one exists, otherwise the default (blank) ScriptableObject icon.
        /// </summary>
        private Texture ResolveRowIcon(Type type)
        {
            if (ScriptableSettingsEditorResolver.TryLoad(type, out ScriptableSettings asset))
            {
                Texture thumbnail = AssetPreview.GetMiniThumbnail(asset);

                if (thumbnail != null)
                {
                    return thumbnail;
                }
            }

            return _settingsIcon;
        }

        private void HandleSelectionChanged(IEnumerable<object> _)
        {
            _list.RefreshItems();
            BuildDetail(SelectedType());
        }

        private Type SelectedType()
        {
            int index = _list.selectedIndex;

            return index >= 0 && index < _types.Count ? _types[index] : null;
        }

        private VisualElement BuildRootFolderFooter()
        {
            VisualElement footer = new VisualElement();
            footer.style.flexShrink = 0;
            footer.style.paddingLeft = SettingsWindowStyles.Space1;
            footer.style.paddingRight = SettingsWindowStyles.Space1;
            footer.style.paddingTop = 3;
            footer.style.paddingBottom = 3;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = SettingsWindowStyles.SeparatorColor;

            Label caption = new Label("Save new created settings at:");
            caption.style.color = SettingsWindowStyles.MutedTextColor;
            caption.style.fontSize = 11;
            caption.style.marginBottom = 2;
            footer.Add(caption);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            TextField pathField = new TextField { value = ScriptableSettingsEditorResolver.RootFolder, isDelayed = true };
            pathField.style.flexGrow = 1;
            pathField.tooltip = "Folder where new settings assets are created. The project is always searched in full.";
            pathField.RegisterValueChangedCallback(changeEvent => ApplyRootFolder(changeEvent.newValue, pathField));
            row.Add(pathField);

            Button browse = new Button(() => BrowseForRoot(pathField)) { text = "\u2026" };
            browse.style.marginLeft = SettingsWindowStyles.Space1;
            row.Add(browse);

            footer.Add(row);

            return footer;
        }

        private void BrowseForRoot(TextField field)
        {
            string current = ScriptableSettingsEditorResolver.RootFolder;
            string start = AssetDatabase.IsValidFolder(current) ? current : "Assets";
            string absolute = EditorUtility.OpenFolderPanel("Folder For New Settings Assets", start, string.Empty);

            if (string.IsNullOrEmpty(absolute))
            {
                return;
            }

            string relative = ToProjectRelative(absolute);

            if (relative == null)
            {
                Debug.LogWarning("The settings folder must be inside the project's 'Assets' folder.");

                return;
            }

            ApplyRootFolder(relative, field);
        }

        private void ApplyRootFolder(string path, TextField field)
        {
            string trimmed = path == null ? string.Empty : path.Trim();

            if (trimmed.Length == 0 || (trimmed != "Assets" && !trimmed.StartsWith("Assets/")))
            {
                Debug.LogWarning("The settings folder must be inside 'Assets'.");
                field.SetValueWithoutNotify(ScriptableSettingsEditorResolver.RootFolder);

                return;
            }

            ScriptableSettingsEditorResolver.RootFolder = trimmed;
            field.SetValueWithoutNotify(trimmed);
        }

        private static string ToProjectRelative(string absolutePath)
        {
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalized = absolutePath.Replace('\\', '/');

            if (normalized == dataPath)
            {
                return "Assets";
            }

            if (normalized.StartsWith(dataPath + "/"))
            {
                return "Assets" + normalized.Substring(dataPath.Length);
            }

            return null;
        }

        private void BuildDetail(Type type)
        {
            _detail.Clear();

            if (type == null)
            {
                ShowPlaceholder("Select a settings type.");

                return;
            }

            VisualElement card = SettingsWindowStyles.CreateCard();
            card.Add(SettingsWindowStyles.CreateTypeHeader("Scriptable Settings", type.Name, type.Namespace));

            if (!ScriptableSettingsEditorResolver.TryLoad(type, out ScriptableSettings asset))
            {
                BuildCreateSection(card, type);
                _detail.Add(card);

                return;
            }

            card.Add(BuildReferencesSection(asset));
            card.Add(BuildPreloadRow(asset));
            BuildDrawerSection(card, type, asset);
            _detail.Add(card);
        }

        private static VisualElement BuildReferencesSection(ScriptableSettings asset)
        {
            VisualElement section = SettingsWindowStyles.CreateSubSection("References");

            MonoScript script = MonoScript.FromScriptableObject(asset);
            section.Add(CreateReadOnlyReference("Script", script, typeof(MonoScript)));
            section.Add(CreateReadOnlyReference("Asset", asset, asset.GetType()));

            return section;
        }

        /// <summary>
        /// A disabled (greyed) <see cref="ObjectField"/> — matching Unity's default Script field — that still pings
        /// and selects the object on click via a transparent overlay sitting above the field.
        /// </summary>
        private static VisualElement CreateReadOnlyReference(string label, UnityEngine.Object value, Type objectType)
        {
            VisualElement container = new VisualElement();
            container.style.position = Position.Relative;

            ObjectField field = new ObjectField(label) { objectType = objectType };
            field.SetValueWithoutNotify(value);
            field.SetEnabled(false);
            container.Add(field);

            if (value != null)
            {
                VisualElement overlay = new VisualElement();
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0;
                overlay.style.top = 0;
                overlay.style.right = 0;
                overlay.style.bottom = 0;
                overlay.tooltip = "Click to select and ping it in the Project window.";
                overlay.RegisterCallback<PointerDownEvent>(_ =>
                {
                    EditorGUIUtility.PingObject(value);
                    Selection.activeObject = value;
                });
                container.Add(overlay);
            }

            return container;
        }

        private void BuildCreateSection(VisualElement card, Type type)
        {
            VisualElement section = SettingsWindowStyles.CreateSubSection("Not Created");

            VisualElement warningRow = new VisualElement();
            warningRow.style.flexDirection = FlexDirection.Row;
            warningRow.style.alignItems = Align.Center;

            Image warningIcon = new Image
            {
                image = EditorGUIUtility.IconContent("console.warnicon").image
            };
            warningIcon.style.width = 16;
            warningIcon.style.height = 16;
            warningIcon.style.marginRight = SettingsWindowStyles.Space1;
            warningRow.Add(warningIcon);

            Label warningLabel = SettingsWindowStyles.CreateInfoLabel("No asset exists for this type yet.");
            warningLabel.style.flexGrow = 1;
            warningRow.Add(warningLabel);

            section.Add(warningRow);

            Button createButton = new Button(() =>
            {
                ScriptableSettingsEditorResolver.Create(type);
                BuildDetail(type);
            })
            {
                text = "Create"
            };
            createButton.style.marginTop = SettingsWindowStyles.Space1;
            createButton.style.alignSelf = Align.FlexStart;
            section.Add(createButton);

            card.Add(section);
        }

        private static VisualElement BuildPreloadRow(ScriptableSettings asset)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = SettingsWindowStyles.Space1;

            Toggle toggle = new Toggle("Preload") { value = asset.Preload };
            toggle.style.flexGrow = 1;
            row.Add(toggle);

            VisualElement pillHost = new VisualElement();
            pillHost.style.flexShrink = 0;
            pillHost.Add(BuildBuildPill(asset.Preload));
            row.Add(pillHost);

            toggle.RegisterValueChangedCallback(changeEvent =>
            {
                ScriptableSettingsPreloadSync.SetPreload(asset, changeEvent.newValue);
                pillHost.Clear();
                pillHost.Add(BuildBuildPill(changeEvent.newValue));
            });

            return row;
        }

        private static VisualElement BuildBuildPill(bool isPreloaded)
        {
            return isPreloaded
                ? SettingsWindowStyles.CreateStatusPill(SettingsWindowStyles.IncludedColor, "In Build")
                : SettingsWindowStyles.CreateStatusPill(SettingsWindowStyles.ExcludedColor, "Excluded");
        }

        private static void BuildDrawerSection(VisualElement card, Type type, ScriptableSettings asset)
        {
            VisualElement section = SettingsWindowStyles.CreateSubSection("Settings");

            SerializedObject serializedObject = new SerializedObject(asset);
            ISettingsDrawer drawer = SettingsDrawerRegistry.GetDrawer(type);
            VisualElement body = drawer.CreateBody(serializedObject);

            body.Bind(serializedObject);
            body.TrackSerializedObjectValue(serializedObject, tracked =>
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            });

            section.Add(body);
            card.Add(section);
        }

        private void ShowPlaceholder(string message)
        {
            Label label = new Label(message);
            label.style.flexGrow = 1;
            label.style.color = SettingsWindowStyles.MutedTextColor;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            _detail.Add(label);
        }
    }
}
