using System.Collections.Generic;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.PackageHandlers;
using AHAKuo.Signalia.GameSystems.AchievementSystem;
using AHAKuo.Signalia.Radio;

using AHAKuo.Signalia.GameSystems.PoolingSystem;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem.Examples
{
    /// <summary>
    /// Viewer component that displays all achievements using the pooling system.
    /// Spawns AchievementSlot instances from a prefab and arranges them in a layout.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Achievement System/Examples/Signalia | Achievement Viewer")]
    public class AchievementViewer : MonoBehaviour
    {
        [Header("Pooling Settings")]
        [Tooltip("Prefab containing an AchievementSlot component to use for displaying achievements.")]
        [SerializeField] private GameObject slotPrefab;
        
        [Tooltip("Transform container where achievement slots will be spawned as children. Should have a LayoutGroup component (Vertical, Horizontal, or Grid Layout Group).")]
        [SerializeField] private Transform slotsContainer;

        [Header("Display Options")]
        [Tooltip("If true, only shows unlocked achievements. If false, shows all achievements.")]
        [SerializeField] private bool showOnlyUnlocked = false;
        
        [Tooltip("If true, sorts achievements with unlocked ones first. If false, uses the order from Signalia Settings.")]
        [SerializeField] private bool sortUnlockedFirst = false;

        [Header("Refresh Settings")]
        [Tooltip("If true, automatically refreshes the display when achievements are unlocked.")]
        [SerializeField] private bool autoRefreshOnUnlock = true;
        
        [Tooltip("Radio event name to listen for achievement unlocks. Leave empty to use default from settings.")]
        [SerializeField] private string unlockEventName = "";

        private List<GameObject> spawnedSlots = new List<GameObject>();
        private Listener unlockListener;

        private bool usePooling = true;

        private void OnEnable()
        {
            RefreshDisplay();
            
            if (autoRefreshOnUnlock)
            {
                SubscribeToUnlockEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromUnlockEvents();
        }

        private void OnDestroy()
        {
            ClearDisplay();
        }

        /// <summary>
        /// Refreshes the achievement display, clearing old slots and creating new ones.
        /// </summary>
        public void RefreshDisplay()
        {
            ClearDisplay();

            if (slotPrefab == null)
            {
                Debug.LogWarning($"[Achievement Viewer] Slot prefab is not assigned on {gameObject.name}.", this);
                return;
            }

            if (slotsContainer == null)
            {
                Debug.LogWarning($"[Achievement Viewer] Slots container is not assigned on {gameObject.name}.", this);
                return;
            }

            // Get all achievements from config
            var config = ConfigReader.GetConfig();
            if (config == null || config.AchievementSystem == null)
            {
                Debug.LogWarning($"[Achievement Viewer] SignaliaConfigAsset or AchievementSystem settings not found.", this);
                return;
            }

            var achievements = config.AchievementSystem.Achievements;
            if (achievements == null || achievements.Length == 0)
            {
                Debug.Log($"[Achievement Viewer] No achievements found in Signalia Settings.", this);
                return;
            }

            // Filter and sort achievements
            List<AchievementSO> achievementsToDisplay = GetAchievementsToDisplay(achievements);

            // Spawn slots for each achievement
            foreach (var achievement in achievementsToDisplay)
            {
                if (achievement == null) continue;

                GameObject slotObject = SpawnSlot();
                if (slotObject == null) continue;

                AchievementSlot slot = slotObject.GetComponent<AchievementSlot>();
                if (slot == null)
                {
                    Debug.LogWarning($"[Achievement Viewer] Slot prefab '{slotPrefab.name}' does not have an AchievementSlot component.", this);
                    ReturnSlotToPool(slotObject);
                    continue;
                }

                // Check unlocked status
                bool unlocked = AchievementManager.IsUnlocked(achievement.Id);
                slot.UpdateSlot(achievement, unlocked);

                spawnedSlots.Add(slotObject);
            }
        }

        /// <summary>
        /// Gets the list of achievements to display based on filtering and sorting options.
        /// </summary>
        private List<AchievementSO> GetAchievementsToDisplay(AchievementSO[] allAchievements)
        {
            List<AchievementSO> result = new List<AchievementSO>();

            foreach (var achievement in allAchievements)
            {
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.Id))
                    continue;

                bool unlocked = AchievementManager.IsUnlocked(achievement.Id);

                // If showOnlyUnlocked is true, only show unlocked achievements
                // Otherwise, show all achievements (both locked and unlocked)
                if (showOnlyUnlocked && !unlocked)
                    continue;

                result.Add(achievement);
            }

            // Sort if requested
            if (sortUnlockedFirst)
            {
                result.Sort((a, b) =>
                {
                    bool aUnlocked = AchievementManager.IsUnlocked(a.Id);
                    bool bUnlocked = AchievementManager.IsUnlocked(b.Id);

                    if (aUnlocked == bUnlocked)
                        return 0;
                    
                    return aUnlocked ? -1 : 1; // Unlocked first
                });
            }

            return result;
        }

        /// <summary>
        /// Spawns a slot GameObject using pooling or instantiation.
        /// </summary>
        private GameObject SpawnSlot()
        {
            if (usePooling)
            {
                GameObject slot = slotPrefab.FromPool(-1f, true);
                if (slot != null)
                {
                    slot.transform.SetParent(slotsContainer, false);
                    return slot;
                }
            }

            // Fallback to instantiation if pooling is not available
            GameObject instantiated = Instantiate(slotPrefab, slotsContainer);
            return instantiated;
        }

        /// <summary>
        /// Returns a slot GameObject to the pool or destroys it.
        /// </summary>
        private void ReturnSlotToPool(GameObject slotObject)
        {
            if (slotObject == null) return;

            if (usePooling)
            {
                // Return to pool by deactivating
                slotObject.SetActive(false);
            }
            else
            {
                // Destroy if not using pooling
                Destroy(slotObject);
            }
        }

        /// <summary>
        /// Clears all displayed achievement slots.
        /// </summary>
        public void ClearDisplay()
        {
            foreach (var slot in spawnedSlots)
            {
                if (slot != null)
                {
                    ReturnSlotToPool(slot);
                }
            }

            spawnedSlots.Clear();
        }

        /// <summary>
        /// Refreshes the unlocked status of all displayed slots without recreating them.
        /// </summary>
        public void RefreshUnlockedStatus()
        {
            foreach (var slotObject in spawnedSlots)
            {
                if (slotObject == null) continue;

                AchievementSlot slot = slotObject.GetComponent<AchievementSlot>();
                if (slot != null)
                {
                    slot.RefreshUnlockedStatus();
                }
            }
        }

        /// <summary>
        /// Subscribes to achievement unlock events to auto-refresh the display.
        /// </summary>
        private void SubscribeToUnlockEvents()
        {
            UnsubscribeFromUnlockEvents();

            var config = ConfigReader.GetConfig();
            if (config == null || config.AchievementSystem == null)
                return;

            // Use custom event name or default from settings
            string eventName = string.IsNullOrWhiteSpace(unlockEventName)
                ? config.AchievementSystem.OnAchievementUnlockedEvent
                : unlockEventName;

            if (!string.IsNullOrWhiteSpace(eventName))
            {
                unlockListener = SIGS.Listener(eventName, OnAchievementUnlocked);
            }
        }

        /// <summary>
        /// Unsubscribes from achievement unlock events.
        /// </summary>
        private void UnsubscribeFromUnlockEvents()
        {
            unlockListener?.Dispose();
            unlockListener = null;
        }

        /// <summary>
        /// Event handler called when an achievement is unlocked.
        /// </summary>
        private void OnAchievementUnlocked()
        {
            if (showOnlyUnlocked)
            {
                // If showing only unlocked, need to refresh entire display to include the newly unlocked achievement
                RefreshDisplay();
            }
            else
            {
                // Otherwise, just refresh unlocked status (viewer always shows all achievements)
                RefreshUnlockedStatus();
            }
        }

        /// <summary>
        /// Sets whether to show only unlocked achievements.
        /// </summary>
        public void SetShowOnlyUnlocked(bool showOnly)
        {
            showOnlyUnlocked = showOnly;
            RefreshDisplay();
        }

        /// <summary>
        /// Sets whether to sort achievements with unlocked ones first.
        /// </summary>
        public void SetSortUnlockedFirst(bool sortFirst)
        {
            sortUnlockedFirst = sortFirst;
            RefreshDisplay();
        }
    }
}
