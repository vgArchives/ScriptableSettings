using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Reconciles the preloaded-assets list at the start of every build, so the shipped list is always correct
    /// regardless of any editor-time drift.
    /// </summary>
    internal sealed class ScriptableSettingsBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ScriptableSettingsPreloadSync.Reconcile();
        }
    }
}
