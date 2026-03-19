using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem
{
    /// <summary>
    /// Optional integration point for pushing unlocked achievements to an external backend (Steam/Console/Custom API).
    /// Create a ScriptableObject deriving from this and reference it in Signalia Settings.
    /// </summary>
    public abstract class AchievementBackendAdapter : ScriptableObject
    {
        /// <summary>
        /// Called when an achievement is newly unlocked.
        /// </summary>
        public abstract void OnAchievementUnlocked(AchievementSO achievement);
    }
}

