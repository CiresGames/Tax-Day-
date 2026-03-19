using System;
using System.Collections.Generic;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Represents a time modifier that affects the scaled time calculation.
    /// Values range from 0 (complete pause) to 1 (no effect).
    /// </summary>
    [Serializable]
    public struct TimeModifier
    {
        /// <summary>
        /// Unique identifier for this modifier.
        /// </summary>
        public string Id;

        /// <summary>
        /// Display name or description of this modifier.
        /// </summary>
        public string Name;

        /// <summary>
        /// The modifier value between 0 and 1.
        /// 0 = Complete pause (time stops)
        /// 1 = No modification (time flows normally)
        /// Values between affect time proportionally.
        /// </summary>
        [Range(0f, 1f)]
        public float Value;

        /// <summary>
        /// Optional source that created this modifier (for debugging).
        /// </summary>
        public string Source;

        /// <summary>
        /// When this modifier was added.
        /// </summary>
        public float CreationTime;

        /// <summary>
        /// Creates a new TimeModifier.
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="name">Display name</param>
        /// <param name="value">Modifier value (0-1)</param>
        /// <param name="source">Optional source identifier</param>
        public TimeModifier(string id, string name, float value, string source = null)
        {
            Id = id;
            Name = name;
            Value = Mathf.Clamp01(value);
            Source = source ?? string.Empty;
            CreationTime = Time.unscaledTime;
        }

        /// <summary>
        /// Creates a simple pause modifier (value = 0).
        /// </summary>
        public static TimeModifier Pause(string id, string name = "Pause", string source = null)
        {
            return new TimeModifier(id, name, 0f, source);
        }

        /// <summary>
        /// Creates a slow motion modifier.
        /// </summary>
        public static TimeModifier SlowMotion(string id, float slowFactor = 0.5f, string source = null)
        {
            return new TimeModifier(id, "Slow Motion", slowFactor, source);
        }

        public override string ToString()
        {
            return $"[{Id}] {Name}: {Value:F2} (Source: {Source})";
        }

        public override bool Equals(object obj)
        {
            if (obj is TimeModifier other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Signalia Time System - Tracks and manages scaled time in the scene.
    /// Automatically added by Watchman when enabled in the config.
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Signalia | Time")]
    public class SignaliaTime : InstancerSingleton<SignaliaTime>
    {
        #region Static Time Modifiers List (Managed by Watchman)

        /// <summary>
        /// Static list of active time modifiers. Managed by Watchman for initialization and reset.
        /// </summary>
        private static List<TimeModifier> timeModifiers = new List<TimeModifier>();

        /// <summary>
        /// Gets a read-only view of the current time modifiers.
        /// </summary>
        public static IReadOnlyList<TimeModifier> Modifiers => timeModifiers.AsReadOnly();

        /// <summary>
        /// The number of active time modifiers.
        /// </summary>
        public static int ModifierCount => timeModifiers.Count;

        #endregion

        #region Scaled Time Properties

        /// <summary>
        /// The aggregate of all time modifiers. This is the final multiplier applied to time.
        /// Calculated as the product of all modifier values.
        /// </summary>
        public static float ModifiersAggregate { get; private set; } = 1f;

        /// <summary>
        /// The current scaled time value (deltaTime * modifiersAggregate).
        /// Use this instead of Time.deltaTime for time-affected systems.
        /// </summary>
        public static float ScaledDeltaTime { get; private set; }

        /// <summary>
        /// The unscaled delta time (always equals Time.unscaledDeltaTime).
        /// </summary>
        public static float UnscaledDeltaTime => Time.unscaledDeltaTime;

        /// <summary>
        /// Total scaled time elapsed since the SignaliaTime was initialized.
        /// </summary>
        public static float TotalScaledTime { get; private set; }

        /// <summary>
        /// Whether time is currently paused (modifiersAggregate == 0).
        /// </summary>
        public static bool IsPaused => Mathf.Approximately(ModifiersAggregate, 0f);

        /// <summary>
        /// Whether time is currently slowed (0 < modifiersAggregate < 1).
        /// </summary>
        public static bool IsSlowed => ModifiersAggregate > 0f && ModifiersAggregate < 1f;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when time is paused (modifiersAggregate becomes 0).
        /// </summary>
        public static event Action OnTimePaused;

        /// <summary>
        /// Event fired when time is resumed (modifiersAggregate becomes > 0 from 0).
        /// </summary>
        public static event Action OnTimeResumed;

        /// <summary>
        /// Event fired when the modifiersAggregate changes.
        /// </summary>
        public static event Action<float> OnTimeScaleChanged;

        #endregion

        #region Private State

        private bool wasPaused = false;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Called by Watchman to add SignaliaTime to the scene.
        /// </summary>
        public static void AddMe()
        {
            if (Instance != null) return;

            var go = new GameObject("Signalia Time");
            go.AddComponent<SignaliaTime>();
        }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            // Initialize values
            ModifiersAggregate = 1f;
            ScaledDeltaTime = 0f;
            TotalScaledTime = 0f;
            wasPaused = false;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            CalculateModifiersAggregate();
            UpdateScaledTime();
            ApplyTimeScale();
            CheckPauseStateChange();
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                // Reset time scale when destroying
                Time.timeScale = 1f;
                // Events are cleared on reset via Watchman
            }
            base.OnDestroy();
        }

        #endregion

        #region Time Calculation

        /// <summary>
        /// Calculates the modifiersAggregate from all active modifiers.
        /// Uses multiplicative blending: aggregate = modifier1 * modifier2 * ... * modifierN
        /// </summary>
        private void CalculateModifiersAggregate()
        {
            float previousAggregate = ModifiersAggregate;

            if (timeModifiers.Count == 0)
            {
                ModifiersAggregate = 1f;
            }
            else
            {
                float aggregate = 1f;
                foreach (var modifier in timeModifiers)
                {
                    aggregate *= modifier.Value;
                }
                ModifiersAggregate = aggregate;
            }

            // Fire event if aggregate changed
            if (!Mathf.Approximately(previousAggregate, ModifiersAggregate))
            {
                OnTimeScaleChanged?.Invoke(ModifiersAggregate);
            }
        }

        /// <summary>
        /// Updates the scaled time values.
        /// </summary>
        private void UpdateScaledTime()
        {
            ScaledDeltaTime = Time.deltaTime * ModifiersAggregate;
            TotalScaledTime += ScaledDeltaTime;
        }

        /// <summary>
        /// Applies the modifiers aggregate to Unity's Time.timeScale.
        /// This ensures Unity's built-in systems (animations, physics, etc.) respect the pause.
        /// </summary>
        private void ApplyTimeScale()
        {
            Time.timeScale = ModifiersAggregate;
        }

        /// <summary>
        /// Checks for pause state changes and fires appropriate events.
        /// </summary>
        private void CheckPauseStateChange()
        {
            bool isPausedNow = IsPaused;

            if (isPausedNow && !wasPaused)
            {
                OnTimePaused?.Invoke();
                SIGS.Send("SignaliaTime_Paused");
            }
            else if (!isPausedNow && wasPaused)
            {
                OnTimeResumed?.Invoke();
                SIGS.Send("SignaliaTime_Resumed");
            }

            wasPaused = isPausedNow;
        }

        #endregion

        #region Modifier Management (Static API)

        /// <summary>
        /// Adds a time modifier to the system.
        /// </summary>
        /// <param name="modifier">The modifier to add</param>
        /// <returns>True if added, false if a modifier with the same ID already exists</returns>
        public static bool AddModifier(TimeModifier modifier)
        {
            Watchman.Watch();

            // Check if modifier with same ID already exists
            for (int i = 0; i < timeModifiers.Count; i++)
            {
                if (timeModifiers[i].Id == modifier.Id)
                {
                    return false;
                }
            }

            timeModifiers.Add(modifier);
            return true;
        }

        /// <summary>
        /// Adds or updates a time modifier. If a modifier with the same ID exists, it will be updated.
        /// </summary>
        /// <param name="modifier">The modifier to add or update</param>
        public static void SetModifier(TimeModifier modifier)
        {
            Watchman.Watch();

            for (int i = 0; i < timeModifiers.Count; i++)
            {
                if (timeModifiers[i].Id == modifier.Id)
                {
                    timeModifiers[i] = modifier;
                    return;
                }
            }

            timeModifiers.Add(modifier);
        }

        /// <summary>
        /// Removes a time modifier by its ID.
        /// </summary>
        /// <param name="id">The ID of the modifier to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public static bool RemoveModifier(string id)
        {
            for (int i = timeModifiers.Count - 1; i >= 0; i--)
            {
                if (timeModifiers[i].Id == id)
                {
                    timeModifiers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes all modifiers from a specific source.
        /// </summary>
        /// <param name="source">The source to remove modifiers from</param>
        /// <returns>Number of modifiers removed</returns>
        public static int RemoveModifiersBySource(string source)
        {
            int removed = 0;
            for (int i = timeModifiers.Count - 1; i >= 0; i--)
            {
                if (timeModifiers[i].Source == source)
                {
                    timeModifiers.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// Checks if a modifier with the given ID exists.
        /// </summary>
        /// <param name="id">The ID to check</param>
        /// <returns>True if exists</returns>
        public static bool HasModifier(string id)
        {
            foreach (var modifier in timeModifiers)
            {
                if (modifier.Id == id) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a modifier by its ID.
        /// </summary>
        /// <param name="id">The ID to find</param>
        /// <returns>The modifier if found, null otherwise</returns>
        public static TimeModifier? GetModifier(string id)
        {
            foreach (var modifier in timeModifiers)
            {
                if (modifier.Id == id) return modifier;
            }
            return null;
        }

        /// <summary>
        /// Clears all time modifiers. Called by Watchman during reset.
        /// </summary>
        public static void ClearModifiers()
        {
            timeModifiers.Clear();
            ModifiersAggregate = 1f;
        }

        /// <summary>
        /// Resets the entire time system. Called by Watchman during cleanup.
        /// </summary>
        public static void Reset()
        {
            ClearModifiers();
            ScaledDeltaTime = 0f;
            TotalScaledTime = 0f;
            Time.timeScale = 1f;
            OnTimePaused = null;
            OnTimeResumed = null;
            OnTimeScaleChanged = null;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Quickly pause time with a simple call.
        /// </summary>
        /// <param name="pauseId">Unique ID for this pause (to allow removal later)</param>
        /// <param name="source">Optional source identifier</param>
        public static void Pause(string pauseId = "GlobalPause", string source = null)
        {
            AddModifier(TimeModifier.Pause(pauseId, "Pause", source));
        }

        /// <summary>
        /// Resume time by removing a specific pause modifier.
        /// </summary>
        /// <param name="pauseId">The ID of the pause to remove</param>
        public static void Resume(string pauseId = "GlobalPause")
        {
            RemoveModifier(pauseId);
        }

        /// <summary>
        /// Resume time by clearing all modifiers.
        /// </summary>
        public static void ResumeAll()
        {
            ClearModifiers();
        }

        #endregion
    }
}
