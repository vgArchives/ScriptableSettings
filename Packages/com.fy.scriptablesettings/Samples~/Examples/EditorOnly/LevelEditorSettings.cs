using Fy.ScriptableSettings;
using UnityEngine;

namespace Fy.ScriptableSettings.Examples
{
    /// <summary>
    /// Editor-only tooling settings used to demonstrate <see cref="EditorOnlySettingsAttribute"/>: it is kept out of
    /// builds and shows under the hub window's "Editor Only" tab.
    /// </summary>
    [EditorOnlySettings]
    public sealed class LevelEditorSettings : ScriptableSettings
    {
        [SerializeField] private float _gridSize = 1f;

        [SerializeField] private bool _snapToGrid = true;

        /// <summary>
        /// Gets the spacing of the level-editor placement grid.
        /// </summary>
        public float GridSize => _gridSize;

        /// <summary>
        /// Gets a value indicating whether placement snaps to the grid.
        /// </summary>
        public bool SnapToGrid => _snapToGrid;
    }
}
