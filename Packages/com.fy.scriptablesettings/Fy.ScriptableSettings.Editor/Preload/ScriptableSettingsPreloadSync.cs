using System;
using System.Collections.Generic;
using Fy.ScriptableSettings;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Keeps the project's preloaded-assets list in sync with each settings type's intent, so that runtime settings
    /// are available in builds. A type is excluded only when it is marked with <see cref="EditorOnlySettingsAttribute"/>.
    /// </summary>
    internal static class ScriptableSettingsPreloadSync
    {
        /// <summary>
        /// Gets a value indicating whether <paramref name="type"/> is editor-only and must be kept out of builds.
        /// </summary>
        internal static bool IsEditorOnly(Type type)
        {
            return type.IsDefined(typeof(EditorOnlySettingsAttribute), inherit: true);
        }

        /// <summary>
        /// Authoritative pass: prunes nulls and duplicates from the preloaded list, then ensures every settings asset
        /// is present if (and only if) its type is not marked <see cref="EditorOnlySettingsAttribute"/>.
        /// </summary>
        internal static void Reconcile()
        {
            List<Object> cleaned = GetCleanedPreloadedAssets();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableSettings)}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableSettings asset = AssetDatabase.LoadAssetAtPath<ScriptableSettings>(path);

                if (asset == null)
                {
                    continue;
                }

                bool isPresent = cleaned.Contains(asset);
                bool include = !IsEditorOnly(asset.GetType());

                if (include && !isPresent)
                {
                    cleaned.Add(asset);
                }
                else if (!include && isPresent)
                {
                    cleaned.RemoveAll(item => item == asset);
                }
            }

            PlayerSettings.SetPreloadedAssets(cleaned.ToArray());
        }

        private static List<Object> GetCleanedPreloadedAssets()
        {
            HashSet<Object> seen = new HashSet<Object>();
            List<Object> cleaned = new List<Object>();

            foreach (Object item in PlayerSettings.GetPreloadedAssets())
            {
                if (item == null)
                {
                    continue;
                }

                if (seen.Add(item))
                {
                    cleaned.Add(item);
                }
            }

            return cleaned;
        }
    }
}
