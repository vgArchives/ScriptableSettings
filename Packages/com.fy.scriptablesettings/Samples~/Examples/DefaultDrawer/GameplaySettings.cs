using Fy.ScriptableSettings;
using UnityEngine;

namespace Fy.ScriptableSettings.Examples
{
    /// <summary>
    /// A few plain fields rendered by the standardized default drawer (no custom drawer needed).
    /// </summary>
    public sealed class GameplaySettings : ScriptableSettings
    {
        [Range(1, 10)]
        [SerializeField] private int _startingLives = 3;

        [Range(0f, 2f)]
        [SerializeField] private float _difficultyMultiplier = 1f;

        [SerializeField] private bool _tutorialEnabled = true;

        /// <summary>
        /// Gets the number of lives the player starts with.
        /// </summary>
        public int StartingLives => _startingLives;

        /// <summary>
        /// Gets the global multiplier applied to difficulty-scaled values.
        /// </summary>
        public float DifficultyMultiplier => _difficultyMultiplier;

        /// <summary>
        /// Gets a value indicating whether the tutorial should run on first launch.
        /// </summary>
        public bool TutorialEnabled => _tutorialEnabled;
    }
}
