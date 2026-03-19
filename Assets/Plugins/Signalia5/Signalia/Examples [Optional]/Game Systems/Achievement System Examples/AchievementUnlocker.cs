using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem.Examples
{
    /// <summary>
    /// Helper MonoBehaviour component for unlocking achievements.
    /// Simply assign an achievement and call UnlockAchievement() to unlock it.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Achievement System/Examples/Signalia | Achievement Unlocker")]
    public class AchievementUnlocker : MonoBehaviour
    {
        [Header("Achievement")]
        [Tooltip("The achievement to unlock when UnlockAchievement() is called.")]
        [SerializeField] private AchievementSO achievement;
        
        [Header("Options")]
        [Tooltip("If true, will check achievement status on Start and log the current state.")]
        [SerializeField] private bool checkStatusOnStart = true;
        
        public void UnlockAchievementNow() => UnlockAchievement(); // just for unity events
        
        private void Start()
        {
            if (checkStatusOnStart && achievement != null)
            {
                bool isUnlocked = IsUnlocked();
                Debug.Log($"[Achievement Unlocker] Achievement '{achievement.Title}' is currently {(isUnlocked ? "UNLOCKED" : "LOCKED")} on {gameObject.name}.", this);
            }
        }

        /// <summary>
        /// Unlocks the assigned achievement.
        /// Returns true if the achievement was successfully unlocked, false otherwise.
        /// If ReshowUnlockedAchievements is enabled, will still show notifications even if already unlocked.
        /// </summary>
        public bool UnlockAchievement()
        {
            if (achievement == null)
            {
                Debug.LogWarning($"[Achievement Unlocker] No achievement assigned on {gameObject.name}. Cannot unlock.", this);
                return false;
            }

            bool wasAlreadyUnlocked = AchievementManager.IsUnlocked(achievement.Id);
            bool unlocked = AchievementManager.Unlock(achievement);
            
            if (unlocked)
            {
                Debug.Log($"[Achievement Unlocker] Successfully unlocked achievement: {achievement.Title}", this);
            }
            else if (wasAlreadyUnlocked)
            {
                // Check if ReshowUnlockedAchievements is enabled
                var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
                bool reshowEnabled = config != null && config.AchievementSystem != null 
                    ? config.AchievementSystem.ReshowUnlockedAchievements 
                    : false;
                
                if (reshowEnabled)
                {
                    Debug.Log($"[Achievement Unlocker] Achievement '{achievement.Title}' was already unlocked. Notification reshown due to ReshowUnlockedAchievements setting.", this);
                }
                else
                {
                    Debug.Log($"[Achievement Unlocker] Achievement '{achievement.Title}' was already unlocked. Enable 'ReshowUnlockedAchievements' in Signalia Settings to reshow notifications.", this);
                }
            }
            else
            {
                Debug.LogWarning($"[Achievement Unlocker] Failed to unlock achievement '{achievement.Title}'. Achievement may not be registered in Signalia Settings.", this);
            }

            return unlocked;
        }

        /// <summary>
        /// Unlocks the assigned achievement by its ID string.
        /// If ReshowUnlockedAchievements is enabled, will still show notifications even if already unlocked.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to unlock</param>
        /// <returns>True if the achievement was successfully unlocked, false otherwise</returns>
        public bool UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                Debug.LogWarning($"[Achievement Unlocker] Empty achievement ID provided on {gameObject.name}. Cannot unlock.", this);
                return false;
            }

            bool wasAlreadyUnlocked = AchievementManager.IsUnlocked(achievementId);
            bool unlocked = AchievementManager.Unlock(achievementId);
            
            if (unlocked)
            {
                Debug.Log($"[Achievement Unlocker] Successfully unlocked achievement with ID: {achievementId}", this);
            }
            else if (wasAlreadyUnlocked)
            {
                // Check if ReshowUnlockedAchievements is enabled
                var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
                bool reshowEnabled = config != null && config.AchievementSystem != null 
                    ? config.AchievementSystem.ReshowUnlockedAchievements 
                    : false;
                
                if (reshowEnabled)
                {
                    Debug.Log($"[Achievement Unlocker] Achievement with ID '{achievementId}' was already unlocked. Notification reshown due to ReshowUnlockedAchievements setting.", this);
                }
                else
                {
                    Debug.Log($"[Achievement Unlocker] Achievement with ID '{achievementId}' was already unlocked. Enable 'ReshowUnlockedAchievements' in Signalia Settings to reshow notifications.", this);
                }
            }
            else
            {
                Debug.LogWarning($"[Achievement Unlocker] Failed to unlock achievement with ID '{achievementId}'. Achievement may not be registered in Signalia Settings.", this);
            }

            return unlocked;
        }

        /// <summary>
        /// Sets the achievement to unlock.
        /// </summary>
        /// <param name="newAchievement">The achievement to assign</param>
        public void SetAchievement(AchievementSO newAchievement)
        {
            achievement = newAchievement;
        }

        /// <summary>
        /// Gets the currently assigned achievement.
        /// </summary>
        /// <returns>The assigned achievement, or null if none is assigned</returns>
        public AchievementSO GetAchievement()
        {
            return achievement;
        }

        /// <summary>
        /// Checks if the assigned achievement is already unlocked.
        /// </summary>
        /// <returns>True if the achievement is unlocked, false otherwise</returns>
        public bool IsUnlocked()
        {
            if (achievement == null || string.IsNullOrWhiteSpace(achievement.Id))
            {
                return false;
            }

            return AchievementManager.IsUnlocked(achievement.Id);
        }

        /// <summary>
        /// Locks (relocks) the assigned achievement.
        /// Returns true if the achievement was successfully locked, false otherwise.
        /// </summary>
        public bool LockAchievement()
        {
            if (achievement == null)
            {
                Debug.LogWarning($"[Achievement Unlocker] No achievement assigned on {gameObject.name}. Cannot lock.", this);
                return false;
            }

            bool locked = AchievementManager.Lock(achievement);
            
            if (locked)
            {
                Debug.Log($"[Achievement Unlocker] Successfully locked achievement: {achievement.Title}", this);
            }
            else
            {
                Debug.Log($"[Achievement Unlocker] Achievement '{achievement.Title}' was already locked or failed to lock.", this);
            }

            return locked;
        }

        /// <summary>
        /// Locks (relocks) the assigned achievement by its ID string.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to lock</param>
        /// <returns>True if the achievement was successfully locked, false otherwise</returns>
        public bool LockAchievement(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                Debug.LogWarning($"[Achievement Unlocker] Empty achievement ID provided on {gameObject.name}. Cannot lock.", this);
                return false;
            }

            bool locked = AchievementManager.Lock(achievementId);
            
            if (locked)
            {
                Debug.Log($"[Achievement Unlocker] Successfully locked achievement with ID: {achievementId}", this);
            }
            else
            {
                Debug.Log($"[Achievement Unlocker] Achievement with ID '{achievementId}' was already locked or failed to lock.", this);
            }

            return locked;
        }

        /// <summary>
        /// Unlocks the achievement if it's locked, or locks it if it's unlocked (toggles the state).
        /// </summary>
        /// <returns>True if the state was changed, false otherwise</returns>
        public bool ToggleAchievement()
        {
            if (achievement == null)
            {
                Debug.LogWarning($"[Achievement Unlocker] No achievement assigned on {gameObject.name}. Cannot toggle.", this);
                return false;
            }

            bool currentlyUnlocked = IsUnlocked();
            
            if (currentlyUnlocked)
            {
                return LockAchievement();
            }
            else
            {
                return UnlockAchievement();
            }
        }
    }
}
