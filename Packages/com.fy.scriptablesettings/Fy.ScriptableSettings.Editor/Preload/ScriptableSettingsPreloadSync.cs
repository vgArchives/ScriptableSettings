using System.Collections.Generic;
using Fy.ScriptableSettings;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Keeps the project's preloaded-assets list in sync with each settings asset's <c>Preload</c> flag, so that
    /// preloaded settings are available at runtime in builds.
    /// </summary>
    internal static class ScriptableSettingsPreloadSync
    {
        private const string PreloadField = "_preload";

        /// <summary>
        /// Writes <paramref name="value"/> to the asset's preload flag, saves it, then immediately adds or removes
        /// the asset from the preloaded-assets list.
        /// </summary>
        internal static void SetPreload(ScriptableSettings asset, bool value)
        {
            if (asset == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty(PreloadField).boolValue = value;
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            ApplyToPreloadedAssets(asset, value);
        }

        /// <summary>
        /// Authoritative pass: prunes nulls and duplicates from the preloaded list, then ensures every settings
        /// asset is present if (and only if) its <c>Preload</c> flag is set.
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

                if (asset.Preload && !isPresent)
                {
                    cleaned.Add(asset);
                }
                else if (!asset.Preload && isPresent)
                {
                    cleaned.RemoveAll(item => item == asset);
                }
            }

            PlayerSettings.SetPreloadedAssets(cleaned.ToArray());
        }

        private static void ApplyToPreloadedAssets(Object asset, bool shouldInclude)
        {
            List<Object> cleaned = GetCleanedPreloadedAssets();
            bool isPresent = cleaned.Contains(asset);

            if (shouldInclude && !isPresent)
            {
                cleaned.Add(asset);
            }
            else if (!shouldInclude && isPresent)
            {
                cleaned.RemoveAll(item => item == asset);
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
