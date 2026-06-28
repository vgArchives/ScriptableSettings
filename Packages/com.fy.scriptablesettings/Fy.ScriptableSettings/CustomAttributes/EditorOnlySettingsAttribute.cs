using System;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Fy.ScriptableSettings
{
    /// <summary>
    /// Marks a <see cref="ScriptableSettings"/> type as editor-only, keeping its asset out of the build's preloaded
    /// assets. Settings without this attribute are preloaded and available at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(ScriptableSettings))]
    [Preserve]
    public sealed class EditorOnlySettingsAttribute : Attribute { }
}
