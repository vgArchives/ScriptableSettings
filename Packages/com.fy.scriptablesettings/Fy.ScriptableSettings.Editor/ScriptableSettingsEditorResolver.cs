using System;
using System.Collections.Generic;
using Fy.ScriptableSettings;
using UnityEditor;
using UnityEngine;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Editor support for the settings system: locating and creating settings assets for the hub window, and
    /// rebuilding the registry from loaded assets when returning to edit mode (so it stays correct even when the
    /// domain reload is disabled).
    /// </summary>
    [InitializeOnLoad]
    internal static class ScriptableSettingsEditorResolver
    {
        /// <summary>The folder used when no custom root has been configured yet.</summary>
        internal const string DefaultRootFolder = "Assets/Settings";

        private const string RootFolderKey = "Fy.ScriptableSettings.RootFolder";

        static ScriptableSettingsEditorResolver()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        /// <summary>
        /// Folder where new settings assets are created (the existing project is searched in full, regardless of
        /// this value). Persisted per-project via <see cref="EditorUserSettings"/>.
        /// </summary>
        internal static string RootFolder
        {
            get
            {
                string stored = EditorUserSettings.GetConfigValue(RootFolderKey);

                return string.IsNullOrEmpty(stored) ? DefaultRootFolder : stored;
            }
            set => EditorUserSettings.SetConfigValue(RootFolderKey, value);
        }

        /// <summary>
        /// Loads the first existing asset of <paramref name="type"/> found anywhere in the project, without creating
        /// one. Warns when more than one asset of the type exists and uses the first.
        /// </summary>
        internal static bool TryLoad(Type type, out ScriptableSettings asset)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");

            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                {
                    WarnDuplicates(type, guids);
                }

                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                asset = (ScriptableSettings)AssetDatabase.LoadAssetAtPath(existingPath, type);

                return asset != null;
            }

            asset = null;

            return false;
        }

        /// <summary>
        /// Creates a new asset of <paramref name="type"/> under <see cref="RootFolder"/>, creating the folder (and
        /// any missing parents) if needed. The instance registers itself via its <c>OnEnable</c> callback.
        /// </summary>
        internal static ScriptableSettings Create(Type type)
        {
            string root = RootFolder;
            EnsureFolder(root);

            ScriptableSettings instance = (ScriptableSettings)ScriptableObject.CreateInstance(type);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{root}/{type.Name}.asset");
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();

            return instance;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void WarnDuplicates(Type type, IReadOnlyList<string> guids)
        {
            List<string> paths = new List<string>(guids.Count);

            foreach (string guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            Debug.LogWarning($"Found {guids.Count} assets of type {type.Name}; using the first.\n" +
                             string.Join("\n", paths));
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            // Rebuild the registry from the currently-loaded settings assets when returning to edit mode. This keeps
            // the registry correct even when "Enter Play Mode Options" disables the domain reload (which would
            // otherwise leave it empty), and drops any transient runtime-created instances from the play session.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                RebuildRegistry();
            }
        }

        private static void RebuildRegistry()
        {
            ScriptableSettingsRegistry.Reset();

            foreach (ScriptableSettings settings in Resources.FindObjectsOfTypeAll<ScriptableSettings>())
            {
                if (EditorUtility.IsPersistent(settings))
                {
                    ScriptableSettingsRegistry.Set(settings.GetType(), settings);
                }
            }
        }
    }
}
