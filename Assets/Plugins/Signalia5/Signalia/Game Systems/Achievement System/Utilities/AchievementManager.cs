using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using UnityEngine;

using AHAKuo.Signalia.GameSystems.SaveSystem;

using AHAKuo.Signalia.GameSystems.Notifications;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem
{
    /// <summary>
    /// Runtime achievement API.
    /// Achievements are defined by <see cref="AchievementSO"/> and referenced from Signalia settings.
    /// </summary>
    public static class AchievementManager
    {
        private static bool _initialized;
        private static readonly Dictionary<string, AchievementSO> _byId = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _unlocked = new(StringComparer.Ordinal);
        private static readonly List<AchievementSO> _ordered = new();

        private static SignaliaConfigAsset Config => ConfigReader.GetConfig();

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            Watchman.Watch();
            _byId.Clear();
            _unlocked.Clear();
            _ordered.Clear();

            var config = Config;
            if (config == null)
            {
                Debug.LogWarning("[Signalia Achievements] SignaliaConfigAsset not found. Achievement lookups may fail.");
                return;
            }

            var settings = config.AchievementSystem;
            if (settings.Achievements == null) return;

            foreach (var achievement in settings.Achievements)
            {
                if (achievement == null) continue;

                string id = (achievement.Id ?? "").Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[Signalia Achievements] Achievement '{achievement.name}' has no ID. It will not be unlockable via ID.");
                    continue;
                }

                if (_byId.ContainsKey(id))
                {
                    Debug.LogWarning($"[Signalia Achievements] Duplicate Achievement ID '{id}'. Only the first entry will be used.");
                    continue;
                }

                _byId.Add(id, achievement);
                _ordered.Add(achievement);

                // Prime cache from save file.
                if (GameSaving.Load(GetSaveKey(id, settings), settings.SaveFileName, false))
                    _unlocked.Add(id);
            }
        }

        private static string GetSaveKey(string achievementId, AchievementSystemSettings settings)
            => $"{settings.SaveKeyPrefix}{achievementId}";

        /// <summary>
        /// Returns a referenced achievement by ID, or null if not found.
        /// </summary>
        public static AchievementSO Get(string achievementId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(achievementId)) return null;
            _byId.TryGetValue(achievementId.Trim(), out var achievement);
            return achievement;
        }

        /// <summary>
        /// Returns whether an achievement is already unlocked.
        /// </summary>
        public static bool IsUnlocked(string achievementId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(achievementId)) return false;
            return _unlocked.Contains(achievementId.Trim());
        }

        /// <summary>
        /// Returns a snapshot list of all unlocked achievement definitions.
        /// </summary>
        public static List<AchievementSO> GetUnlockedAchievements()
        {
            EnsureInitialized();

            var unlocked = new List<AchievementSO>(_unlocked.Count);
            for (int i = 0; i < _ordered.Count; i++)
            {
                var achievement = _ordered[i];
                if (achievement == null) continue;

                var id = achievement.Id;
                if (string.IsNullOrEmpty(id)) continue;

                if (_unlocked.Contains(id))
                    unlocked.Add(achievement);
            }

            return unlocked;
        }

        /// <summary>
        /// Marks an achievement as unlocked by its ID.
        /// Returns true only when the achievement was newly unlocked.
        /// If ReshowUnlockedAchievements is enabled, will still trigger notifications/events even if already unlocked.
        /// </summary>
        public static bool Unlock(string achievementId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(achievementId))
            {
                Debug.LogWarning("[Signalia Achievements] Unlock called with an empty ID.");
                return false;
            }

            achievementId = achievementId.Trim();
            bool wasAlreadyUnlocked = _unlocked.Contains(achievementId);

            var config = Config;
            if (config == null)
                return false;

            var settings = config.AchievementSystem;
            var achievement = Get(achievementId);
            if (achievement == null)
            {
                Debug.LogWarning($"[Signalia Achievements] Unknown achievement ID '{achievementId}'. Make sure it's referenced in Signalia Settings.");
                return false;
            }

            // If already unlocked, check if we should reshow notifications
            if (wasAlreadyUnlocked)
            {
                if (settings.ReshowUnlockedAchievements)
                {
                    // Reshow notifications/events even though it's already unlocked
                    TriggerUnlockEvents(achievement, settings);
                }
                return false; // Still return false since it wasn't newly unlocked
            }

            // Newly unlocking the achievement
            _unlocked.Add(achievementId);

            GameSaving.Save(GetSaveKey(achievementId, settings), true, settings.SaveFileName);

            // Trigger unlock events
            TriggerUnlockEvents(achievement, settings);

            return true;
        }

        /// <summary>
        /// Triggers all unlock-related events (backend, notifications, radio) for an achievement.
        /// </summary>
        private static void TriggerUnlockEvents(AchievementSO achievement, AchievementSystemSettings settings)
        {
            // Backend hook (optional)
            if (settings.BackendAdapter != null)
            {
                try { settings.BackendAdapter.OnAchievementUnlocked(achievement); }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            // Notification (optional)
            if (settings.ShowNotifications)
            {
                string message = string.IsNullOrWhiteSpace(settings.NotificationFormat)
                    ? $"Achievement Unlocked: {achievement.Title}"
                    : string.Format(settings.NotificationFormat, achievement.Title);

                NotificationMethods.ShowNotification(settings.NotificationSystemMessageName, message);
            }

            // Radio event
            if (!string.IsNullOrWhiteSpace(settings.OnAchievementUnlockedEvent))
                SimpleRadio.SendEvent(settings.OnAchievementUnlockedEvent, achievement);
        }

        /// <summary>
        /// Marks an achievement as unlocked by reference.
        /// Returns true only when the achievement was newly unlocked.
        /// </summary>
        public static bool Unlock(AchievementSO achievement)
        {
            if (achievement == null) return false;
            return Unlock(achievement.Id);
        }

        /// <summary>
        /// Marks an achievement as locked (relocked) by its ID.
        /// Returns true only when the achievement was previously unlocked and is now locked.
        /// </summary>
        public static bool Lock(string achievementId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(achievementId))
            {
                Debug.LogWarning("[Signalia Achievements] Lock called with an empty ID.");
                return false;
            }

            achievementId = achievementId.Trim();
            if (!_unlocked.Contains(achievementId))
                return false; // Already locked

            var config = Config;
            if (config == null)
                return false;

            var settings = config.AchievementSystem;
            var achievement = Get(achievementId);
            if (achievement == null)
            {
                Debug.LogWarning($"[Signalia Achievements] Unknown achievement ID '{achievementId}'. Make sure it's referenced in Signalia Settings.");
                return false;
            }

            _unlocked.Remove(achievementId);

            GameSaving.Save(GetSaveKey(achievementId, settings), false, settings.SaveFileName);

            // Radio event for locking (if needed)
            if (!string.IsNullOrWhiteSpace(settings.OnAchievementUnlockedEvent))
            {
                // Optionally send a lock event - you might want to add a separate event name for this
                // For now, we'll just remove it from unlocked state
            }

            return true;
        }

        /// <summary>
        /// Marks an achievement as locked (relocked) by reference.
        /// Returns true only when the achievement was previously unlocked and is now locked.
        /// </summary>
        public static bool Lock(AchievementSO achievement)
        {
            if (achievement == null) return false;
            return Lock(achievement.Id);
        }

        /// <summary>
        /// Clears runtime caches. Next query will re-read config (and saved unlock states if available).
        /// </summary>
        public static void ResetCache()
        {
            _initialized = false;
            _byId.Clear();
            _unlocked.Clear();
            _ordered.Clear();
        }
    }
}

