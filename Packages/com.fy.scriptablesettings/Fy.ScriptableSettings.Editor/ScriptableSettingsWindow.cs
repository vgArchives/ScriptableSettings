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
    /// Single hub window for every <see cref="ScriptableSettings"/> type. The left pane splits discovered types into a
    /// "Runtime" tab (preloaded into builds) and an "Editor Only" tab (marked with <see cref="EditorOnlySettingsAttribute"/>);
    /// the right pane shows a standardized header, a read-only build indicator, and either the type's drawer body or a
    /// Create button when no asset exists yet.
    /// </summary>
    public sealed class ScriptableSettingsWindow : EditorWindow
    {
        private readonly List<Type> _runtimeTypes = new();
        private readonly List<Type> _editorOnlyTypes = new();

        private ListView _runtimeList;
        private ListView _editorOnlyList;
        private ScrollView _detail;
        private Texture _settingsIcon;
        private Type _selectedType;

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

            _runtimeList = BuildList(_runtimeTypes);
            _editorOnlyList = BuildList(_editorOnlyTypes);

            TabView tabs = new TabView();
            tabs.style.flexGrow = 1;
            tabs.Add(BuildListTab("Game Settings", _runtimeList));
            tabs.Add(BuildListTab("Editor Settings", _editorOnlyList));
            StretchTabHeaders(tabs);
            tabs.activeTabChanged += (_, _) => HandleTabChanged(tabs);

            leftPane.Add(tabs);
            leftPane.Add(BuildRootFolderFooter());
            split.Add(leftPane);

            _detail = new ScrollView();
            _detail.style.flexGrow = 1;
            split.Add(_detail);

            rootVisualElement.Add(split);

            if (_runtimeTypes.Count > 0)
            {
                _runtimeList.selectedIndex = 0;
            }
            else if (_editorOnlyTypes.Count > 0)
            {
                ShowPlaceholder("Select a settings type.");
            }
            else
            {
                ShowPlaceholder("No settings types found.");
            }

            EditorApplication.projectChanged -= HandleProjectChanged;
            EditorApplication.projectChanged += HandleProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        /// <summary>
        /// Keeps the window in sync when assets change outside it (e.g. an asset is deleted in the Project window):
        /// refreshes the row icons and re-evaluates the current selection so it flips to its create/missing state.
        /// </summary>
        private void HandleProjectChanged()
        {
            if (_runtimeList == null)
            {
                return;
            }

            _runtimeList.RefreshItems();
            _editorOnlyList.RefreshItems();
            BuildDetail(_selectedType);
        }

        private void RefreshTypes()
        {
            _runtimeTypes.Clear();
            _editorOnlyTypes.Clear();

            IEnumerable<Type> discovered = TypeCache.GetTypesDerivedFrom<ScriptableSettings>()
                .Where(type => !type.IsAbstract && !type.IsGenericType && !IsTestAssembly(type.Assembly))
                .OrderBy(type => type.Name);

            foreach (Type type in discovered)
            {
                if (ScriptableSettingsPreloadSync.IsEditorOnly(type))
                {
                    _editorOnlyTypes.Add(type);
                }
                else
                {
                    _runtimeTypes.Add(type);
                }
            }
        }

        private ListView BuildList(List<Type> source)
        {
            ListView list = new ListView
            {
                itemsSource = source,
                fixedItemHeight = 22f,
                selectionType = SelectionType.Single,
                makeItem = MakeRow
            };
            list.style.flexGrow = 1;
            list.bindItem = (element, index) => BindRow(list, source, element, index);
            list.selectionChanged += _ => HandleSelectionChanged(list, source);

            return list;
        }

        private static Tab BuildListTab(string label, ListView list)
        {
            Tab tab = new Tab(label);
            tab.Add(list);

            return tab;
        }

        /// <summary>
        /// Makes the two tab headers share the full width of the left section, each centered.
        /// </summary>
        private static void StretchTabHeaders(TabView tabs)
        {
            tabs.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                foreach (VisualElement header in tabs.Query<VisualElement>(className: "unity-tab__header").ToList())
                {
                    header.style.flexGrow = 1;
                    header.style.justifyContent = Justify.Center;
                }
            });
        }

        private void HandleTabChanged(TabView tabs)
        {
            bool editorTab = tabs.selectedTabIndex == 1;
            ListView list = editorTab ? _editorOnlyList : _runtimeList;
            List<Type> source = editorTab ? _editorOnlyTypes : _runtimeTypes;

            if (source.Count > 0)
            {
                list.selectedIndex = 0;
                BuildDetail(source[0]);
            }
            else
            {
                list.ClearSelection();
                BuildDetail(null);
            }
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

        private void BindRow(ListView list, List<Type> source, VisualElement element, int index)
        {
            bool selected = index == list.selectedIndex;
            Type type = source[index];
            element.Q<Image>("icon").image = ResolveRowIcon(type);
            element.Q<Label>("label").text = ObjectNames.NicifyVariableName(type.Name);
            element.style.backgroundColor = selected
                ? SettingsWindowStyles.SelectedRowColor
                : index % 2 == 0 ? SettingsWindowStyles.RowDarkColor : SettingsWindowStyles.RowLightColor;
            element.Q("marker").style.visibility = selected ? Visibility.Visible : Visibility.Hidden;
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

        private void HandleSelectionChanged(ListView list, List<Type> source)
        {
            list.RefreshItems();
            BuildDetail(SelectedType(list, source));
        }

        private static Type SelectedType(ListView list, List<Type> source)
        {
            int index = list.selectedIndex;

            return index >= 0 && index < source.Count ? source[index] : null;
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
            _selectedType = type;
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
            BuildDrawerSection(card, type, asset);
            _detail.Add(card);
        }

        private static VisualElement BuildReferencesSection(ScriptableSettings asset)
        {
            VisualElement section = SettingsWindowStyles.CreateSubSection("References");

            MonoScript script = MonoScript.FromScriptableObject(asset);
            section.Add(CreateReadOnlyReference("Script", script, typeof(MonoScript)));
            section.Add(CreateReadOnlyReference("Asset", asset, asset.GetType()));
            section.Add(BuildBuildStatusField(asset.GetType()));

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
                ScriptableSettingsPreloadSync.Reconcile();
                _runtimeList.RefreshItems();
                _editorOnlyList.RefreshItems();
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

        /// <summary>
        /// A read-only field styled like the Script/Asset object fields, so the label column lines up with them.
        /// </summary>
        private static VisualElement BuildBuildStatusField(Type type)
        {
            VisualElement field = new VisualElement();
            field.AddToClassList(BaseField<bool>.ussClassName);
            field.style.flexDirection = FlexDirection.Row;
            field.style.alignItems = Align.Center;

            Label label = new Label("Build Status");
            label.AddToClassList(BaseField<bool>.labelUssClassName);
            field.Add(label);

            VisualElement input = new VisualElement();
            input.AddToClassList(BaseField<bool>.inputUssClassName);
            input.style.flexDirection = FlexDirection.Row;
            input.style.alignItems = Align.Center;
            input.Add(BuildBuildPill(!ScriptableSettingsPreloadSync.IsEditorOnly(type)));
            field.Add(input);

            return field;
        }

        private static VisualElement BuildBuildPill(bool isPreloaded)
        {
            return isPreloaded
                ? SettingsWindowStyles.CreateStatusPill(SettingsWindowStyles.IncludedColor, "Preloaded")
                : SettingsWindowStyles.CreateStatusPill(SettingsWindowStyles.EditorOnlyColor, "Editor Only");
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
