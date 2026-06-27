using Fy.ScriptableSettings;
using UnityEngine;

namespace Fy.ScriptableSettings.Examples
{
    /// <summary>
    /// End-to-end proof: reads a preloaded setting at runtime through the registry, with no manual wiring.
    /// </summary>
    public sealed class SettingsReader : MonoBehaviour
    {
        private void Start()
        {
            if (ScriptableSettingsRegistry.TryGet(out GameplaySettings gameplay))
            {
                Debug.Log($"Gameplay loaded: startingLives={gameplay.StartingLives}, " +
                          $"difficulty={gameplay.DifficultyMultiplier}, tutorial={gameplay.TutorialEnabled}");
            }
            else
            {
                Debug.LogWarning("GameplaySettings is not registered. Make sure its asset exists and is preloaded.");
            }
        }
    }
}
