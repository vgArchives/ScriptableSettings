using System.Collections.Generic;
using System.Linq;
using Fy.ScriptableSettings;
using Fy.ScriptableSettings.Editor;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Fy.ScriptableSettings.EditorTests
{
    public sealed class PreloadAndResolverTests
    {
        private Object[] _originalPreloaded;
        private readonly List<string> _createdAssetPaths = new();

        [SetUp]
        public void SetUp()
        {
            _originalPreloaded = PlayerSettings.GetPreloadedAssets();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerSettings.SetPreloadedAssets(_originalPreloaded);

            foreach (string path in _createdAssetPaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            _createdAssetPaths.Clear();
            AssetDatabase.Refresh();
        }

        [Test]
        public void Create_PlacesAssetUnderSettingsFolder()
        {
            ScriptableSettings asset = CreateAsset();
            string path = AssetDatabase.GetAssetPath(asset);

            Assert.IsTrue(AssetDatabase.IsValidFolder(ScriptableSettingsEditorResolver.RootFolder));
            Assert.IsTrue(path.StartsWith($"{ScriptableSettingsEditorResolver.RootFolder}/"));
            Assert.IsTrue(path.EndsWith(".asset"));
        }

        [Test]
        public void Reconcile_PrunesNullAndDeduplicates()
        {
            ScriptableSettings asset = CreateAsset();
            PlayerSettings.SetPreloadedAssets(new Object[] { null, asset, asset });

            ScriptableSettingsPreloadSync.Reconcile();

            Object[] result = PlayerSettings.GetPreloadedAssets();
            Assert.IsFalse(result.Any(item => item == null));
            Assert.AreEqual(1, result.Count(item => item == asset));
        }

        [Test]
        public void Reconcile_AddsNonEditorOnlyAsset()
        {
            ScriptableSettings asset = CreateAsset(typeof(PreloadTestSettings));
            PlayerSettings.SetPreloadedAssets(new Object[0]);

            ScriptableSettingsPreloadSync.Reconcile();

            Assert.IsTrue(PlayerSettings.GetPreloadedAssets().Contains(asset));
        }

        [Test]
        public void Reconcile_RemovesEditorOnlyAsset()
        {
            ScriptableSettings asset = CreateAsset(typeof(EditorOnlyTestSettings));
            PlayerSettings.SetPreloadedAssets(new Object[] { asset });

            ScriptableSettingsPreloadSync.Reconcile();

            Assert.IsFalse(PlayerSettings.GetPreloadedAssets().Contains(asset));
        }

        [Test]
        public void IsEditorOnly_MatchesAttribute()
        {
            Assert.IsTrue(ScriptableSettingsPreloadSync.IsEditorOnly(typeof(EditorOnlyTestSettings)));
            Assert.IsFalse(ScriptableSettingsPreloadSync.IsEditorOnly(typeof(PreloadTestSettings)));
        }

        private ScriptableSettings CreateAsset()
        {
            return CreateAsset(typeof(PreloadTestSettings));
        }

        private ScriptableSettings CreateAsset(System.Type type)
        {
            ScriptableSettings asset = ScriptableSettingsEditorResolver.Create(type);
            _createdAssetPaths.Add(AssetDatabase.GetAssetPath(asset));

            return asset;
        }
    }
}
