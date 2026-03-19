using System;
using System.Collections.Generic;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Utilities.SIGInput
{
    /// <summary>
    /// Represents an input state modifier that affects action maps, actions, and cursor visibility.
    /// Multiple modifiers can be active simultaneously - the system uses union logic for blocking
    /// and "any wants visible" logic for cursor.
    /// </summary>
    [Serializable]
    public struct InputStateModifier
    {
        /// <summary>
        /// Unique identifier for this modifier.
        /// </summary>
        public string Id;

        /// <summary>
        /// Optional source that created this modifier (for debugging).
        /// </summary>
        public string Source;

        /// <summary>
        /// Action map names to disable while this modifier is active.
        /// </summary>
        public string[] BlockedActionMaps;

        /// <summary>
        /// Individual action names to disable while this modifier is active.
        /// </summary>
        public string[] BlockedActions;

        /// <summary>
        /// Action map names that should be force-enabled (removed from disabled set).
        /// Useful for systems like Dialogue that need to ensure GUI maps remain available.
        /// </summary>
        public string[] ForceEnabledActionMaps;

        /// <summary>
        /// If true, this modifier wants the cursor to be visible.
        /// Cursor is shown if ANY active modifier wants it visible.
        /// </summary>
        public bool ShowCursor;

        /// <summary>
        /// The cursor lock state this modifier wants when cursor is hidden.
        /// Only applied when no modifier wants cursor visible.
        /// </summary>
        public CursorLockMode HiddenCursorLockState;

        /// <summary>
        /// Priority for cursor state conflicts. Higher priority wins.
        /// Default is 0.
        /// </summary>
        public int Priority;

        /// <summary>
        /// Creates a new InputStateModifier.
        /// </summary>
        public InputStateModifier(
            string id,
            string source = null,
            string[] blockedActionMaps = null,
            string[] blockedActions = null,
            bool showCursor = false,
            CursorLockMode hiddenCursorLockState = CursorLockMode.Locked,
            int priority = 0,
            string[] forceEnabledActionMaps = null)
        {
            Id = id;
            Source = source ?? string.Empty;
            BlockedActionMaps = blockedActionMaps ?? Array.Empty<string>();
            BlockedActions = blockedActions ?? Array.Empty<string>();
            ForceEnabledActionMaps = forceEnabledActionMaps ?? Array.Empty<string>();
            ShowCursor = showCursor;
            HiddenCursorLockState = hiddenCursorLockState;
            Priority = priority;
        }

        /// <summary>
        /// Creates a modifier that blocks action maps and shows cursor (typical for UI menus).
        /// </summary>
        public static InputStateModifier ForUIView(string id, string[] blockedActionMaps, string[] blockedActions, bool showCursor, string source = null)
        {
            return new InputStateModifier(id, source, blockedActionMaps, blockedActions, showCursor, CursorLockMode.Locked, 0, null);
        }

        /// <summary>
        /// Creates a modifier for dialogue-like systems that blocks gameplay and ensures UI maps are available.
        /// </summary>
        public static InputStateModifier ForDialogue(string id, string[] blockedActionMaps, string[] forceEnabledActionMaps, bool showCursor, string source = null, int priority = 10)
        {
            return new InputStateModifier(id, source, blockedActionMaps, null, showCursor, CursorLockMode.Locked, priority, forceEnabledActionMaps);
        }

        /// <summary>
        /// Creates a simple cursor visibility modifier.
        /// </summary>
        public static InputStateModifier CursorOnly(string id, bool showCursor, string source = null, int priority = 0)
        {
            return new InputStateModifier(id, source, null, null, showCursor, CursorLockMode.Locked, priority, null);
        }

        public override string ToString()
        {
            return $"[{Id}] Source: {Source}, ShowCursor: {ShowCursor}, BlockedMaps: {BlockedActionMaps?.Length ?? 0}, BlockedActions: {BlockedActions?.Length ?? 0}, ForceEnabled: {ForceEnabledActionMaps?.Length ?? 0}";
        }

        public override bool Equals(object obj)
        {
            if (obj is InputStateModifier other)
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

    public static class SignaliaInputBridge
    {
        private static bool IsInputSystemEnabled()
        {
            var config = ConfigReader.GetConfig();
            return config == null || config.InputSystem.EnableSignaliaInputSystem;
        }

        #region Input State Modifiers

        /// <summary>
        /// Static list of active input state modifiers.
        /// </summary>
        private static readonly List<InputStateModifier> inputModifiers = new List<InputStateModifier>();

        /// <summary>
        /// Gets a read-only view of the current input state modifiers.
        /// </summary>
        public static IReadOnlyList<InputStateModifier> Modifiers => inputModifiers.AsReadOnly();

        /// <summary>
        /// The number of active input state modifiers.
        /// </summary>
        public static int ModifierCount => inputModifiers.Count;

        /// <summary>
        /// Dirty flag to indicate modifiers have changed and need reprocessing.
        /// </summary>
        private static bool modifiersDirty = true;

        /// <summary>
        /// Cached computed state: action maps that should be disabled based on all modifiers.
        /// </summary>
        private static readonly HashSet<string> ComputedDisabledActionMaps = new HashSet<string>();

        /// <summary>
        /// Cached computed state: actions that should be disabled based on all modifiers.
        /// </summary>
        private static readonly HashSet<string> ComputedDisabledActions = new HashSet<string>();

        /// <summary>
        /// Cached computed state: whether cursor should be visible based on all modifiers.
        /// </summary>
        private static bool computedCursorVisible = false;

        /// <summary>
        /// Cached computed state: cursor lock mode when hidden.
        /// </summary>
        private static CursorLockMode computedHiddenLockState = CursorLockMode.Locked;

        /// <summary>
        /// Last applied cursor visibility state (to avoid redundant Unity calls).
        /// </summary>
        private static bool lastAppliedCursorVisible = true;

        /// <summary>
        /// Last applied cursor lock state (to avoid redundant Unity calls).
        /// </summary>
        private static CursorLockMode lastAppliedCursorLockState = CursorLockMode.None;

        #endregion

        private static readonly Dictionary<string, SignaliaInputState> InputStates = new();
        private static readonly Dictionary<string, SignaliaActionType> ActionTypes = new();
        private static readonly Dictionary<string, HashSet<string>> ActionToMapNames = new(); // Maps action name to all action map names that contain it
        private static readonly HashSet<string> KnownMapNames = new();
        private static readonly Dictionary<string, HashSet<string>> MapToBlockedMapNames = new(); // Map name -> map names that are suppressed when this map is enabled
        private static readonly HashSet<string> DisabledActions = new();
        private static readonly HashSet<string> DisabledActionMaps = new();
        private static readonly HashSet<string> BaseDisabledActionMaps = new(); // Initial disabled maps from config, preserved across modifier processing
        private static readonly HashSet<string> BaseDisabledActions = new(); // Initial disabled actions, preserved across modifier processing
        private static readonly Dictionary<string, CooldownState> Cooldowns = new();
        private static readonly Dictionary<string, BufferedInputState> BufferedInputs = new();
        private static bool inputDisabled;
        private static bool actionMapsCached;
        private static bool initialActionMapStatesApplied;
        private static bool mapSuppressionDirty = true;
        private static readonly HashSet<string> SuppressedMapsCache = new();

        public static void Reset()
        {
            InputStates.Clear();
            ActionTypes.Clear();
            ActionToMapNames.Clear();
            KnownMapNames.Clear();
            MapToBlockedMapNames.Clear();
            DisabledActions.Clear();
            DisabledActionMaps.Clear();
            BaseDisabledActionMaps.Clear();
            BaseDisabledActions.Clear();
            Cooldowns.Clear();
            BufferedInputs.Clear();
            inputDisabled = false;
            actionMapsCached = false;
            initialActionMapStatesApplied = false;
            mapSuppressionDirty = true;
            SuppressedMapsCache.Clear();
            
            // Reset modifier system
            inputModifiers.Clear();
            modifiersDirty = true;
            ComputedDisabledActionMaps.Clear();
            ComputedDisabledActions.Clear();
            ComputedForceEnabledActionMaps.Clear();
            computedCursorVisible = false;
            computedHiddenLockState = CursorLockMode.Locked;
            lastAppliedCursorVisible = true;
            lastAppliedCursorLockState = CursorLockMode.None;
        }

        #region Modifier Management

        /// <summary>
        /// Adds an input state modifier to the system.
        /// </summary>
        /// <param name="modifier">The modifier to add</param>
        /// <returns>True if added, false if a modifier with the same ID already exists</returns>
        public static bool AddModifier(InputStateModifier modifier)
        {
            Watchman.Watch();

            // Check if modifier with same ID already exists
            for (int i = 0; i < inputModifiers.Count; i++)
            {
                if (inputModifiers[i].Id == modifier.Id)
                {
                    return false;
                }
            }

            inputModifiers.Add(modifier);
            modifiersDirty = true;
            return true;
        }

        /// <summary>
        /// Adds or updates an input state modifier. If a modifier with the same ID exists, it will be replaced.
        /// </summary>
        /// <param name="modifier">The modifier to add or update</param>
        public static void SetModifier(InputStateModifier modifier)
        {
            Watchman.Watch();

            for (int i = 0; i < inputModifiers.Count; i++)
            {
                if (inputModifiers[i].Id == modifier.Id)
                {
                    inputModifiers[i] = modifier;
                    modifiersDirty = true;
                    return;
                }
            }

            inputModifiers.Add(modifier);
            modifiersDirty = true;
        }

        /// <summary>
        /// Removes an input state modifier by its ID.
        /// </summary>
        /// <param name="id">The ID of the modifier to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public static bool RemoveModifier(string id)
        {
            for (int i = inputModifiers.Count - 1; i >= 0; i--)
            {
                if (inputModifiers[i].Id == id)
                {
                    inputModifiers.RemoveAt(i);
                    modifiersDirty = true;
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
            for (int i = inputModifiers.Count - 1; i >= 0; i--)
            {
                if (inputModifiers[i].Source == source)
                {
                    inputModifiers.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0)
            {
                modifiersDirty = true;
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
            for (int i = 0; i < inputModifiers.Count; i++)
            {
                if (inputModifiers[i].Id == id) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a modifier by its ID.
        /// </summary>
        /// <param name="id">The ID to find</param>
        /// <returns>The modifier if found, null otherwise</returns>
        public static InputStateModifier? GetModifier(string id)
        {
            for (int i = 0; i < inputModifiers.Count; i++)
            {
                if (inputModifiers[i].Id == id) return inputModifiers[i];
            }
            return null;
        }

        /// <summary>
        /// Clears all input state modifiers.
        /// </summary>
        public static void ClearModifiers()
        {
            inputModifiers.Clear();
            modifiersDirty = true;
        }

        #endregion

        #region Update Loop Processing

        /// <summary>
        /// Cached computed state: action maps that should be force-enabled (removed from disabled set).
        /// </summary>
        private static readonly HashSet<string> ComputedForceEnabledActionMaps = new HashSet<string>();

        /// <summary>
        /// Processes all input state modifiers and applies the computed state.
        /// Should be called every frame from Watchman/InputDelegation.Update().
        /// </summary>
        public static void ProcessModifiers()
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            var config = ConfigReader.GetConfig();
            var defaultVisibilityMode = config?.InputSystem.CursorVisibility.DefaultVisibilityMode
                ?? CursorVisibilityDefaultMode.VisibleUnlessModified;
            bool hasModifiers = inputModifiers.Count > 0;

            if (!modifiersDirty)
            {
                if (!hasModifiers && defaultVisibilityMode == CursorVisibilityDefaultMode.OnlyVisibleOnAnyMenu)
                {
                    bool shouldBeVisible = IsAnyMenuVisible();
                    if (computedCursorVisible != shouldBeVisible)
                    {
                        computedCursorVisible = shouldBeVisible;
                        computedHiddenLockState = CursorLockMode.Locked;
                        ApplyCursorState();
                    }
                }

                return;
            }

            modifiersDirty = false;

            // Compute disabled action maps and actions from all modifiers
            ComputedDisabledActionMaps.Clear();
            ComputedDisabledActions.Clear();
            ComputedForceEnabledActionMaps.Clear();
            computedCursorVisible = false;
            computedHiddenLockState = CursorLockMode.Locked;
            int highestPriority = int.MinValue;

            for (int i = 0; i < inputModifiers.Count; i++)
            {
                var modifier = inputModifiers[i];

                // Union all blocked action maps
                if (modifier.BlockedActionMaps != null)
                {
                    for (int j = 0; j < modifier.BlockedActionMaps.Length; j++)
                    {
                        var mapName = modifier.BlockedActionMaps[j];
                        if (!string.IsNullOrWhiteSpace(mapName))
                        {
                            ComputedDisabledActionMaps.Add(mapName);
                        }
                    }
                }

                // Union all blocked actions
                if (modifier.BlockedActions != null)
                {
                    for (int j = 0; j < modifier.BlockedActions.Length; j++)
                    {
                        var actionName = modifier.BlockedActions[j];
                        if (!string.IsNullOrWhiteSpace(actionName))
                        {
                            ComputedDisabledActions.Add(actionName);
                        }
                    }
                }

                // Union all force-enabled action maps
                if (modifier.ForceEnabledActionMaps != null)
                {
                    for (int j = 0; j < modifier.ForceEnabledActionMaps.Length; j++)
                    {
                        var mapName = modifier.ForceEnabledActionMaps[j];
                        if (!string.IsNullOrWhiteSpace(mapName))
                        {
                            ComputedForceEnabledActionMaps.Add(mapName);
                        }
                    }
                }

                // Cursor: if ANY modifier wants cursor visible, show it
                if (modifier.ShowCursor)
                {
                    computedCursorVisible = true;
                }

                // Track highest priority for hidden lock state
                if (modifier.Priority > highestPriority)
                {
                    highestPriority = modifier.Priority;
                    computedHiddenLockState = modifier.HiddenCursorLockState;
                }
            }

            if (!hasModifiers)
            {
                computedCursorVisible = GetDefaultCursorVisibility(defaultVisibilityMode);
                computedHiddenLockState = CursorLockMode.Locked;
            }

            // Apply computed state to the existing disabled sets
            ApplyComputedState();

            // Apply cursor state
            ApplyCursorState();
        }

        /// <summary>
        /// Applies the computed disabled maps/actions to the system.
        /// Combines base disabled state (from initial config) with modifier-computed state.
        /// Force-enabled maps take priority over blocked maps.
        /// </summary>
        private static void ApplyComputedState()
        {
            // Sync DisabledActionMaps with computed state
            // Start with base disabled maps, then add modifier-blocked maps
            // Force-enabled maps override both
            DisabledActionMaps.Clear();
            
            // Add base disabled maps (from initial config) unless force-enabled
            foreach (var mapName in BaseDisabledActionMaps)
            {
                if (!ComputedForceEnabledActionMaps.Contains(mapName))
                {
                    DisabledActionMaps.Add(mapName);
                }
            }
            
            // Add modifier-blocked maps unless force-enabled
            foreach (var mapName in ComputedDisabledActionMaps)
            {
                if (!ComputedForceEnabledActionMaps.Contains(mapName))
                {
                    DisabledActionMaps.Add(mapName);
                }
            }

            // Sync DisabledActions with computed state
            // Start with base disabled actions, then add modifier-blocked actions
            DisabledActions.Clear();
            
            // Add base disabled actions (if any)
            foreach (var actionName in BaseDisabledActions)
            {
                DisabledActions.Add(actionName);
            }
            
            // Add modifier-blocked actions
            foreach (var actionName in ComputedDisabledActions)
            {
                DisabledActions.Add(actionName);
            }

            // Mark map suppression as dirty since disabled maps changed
            mapSuppressionDirty = true;
        }

        /// <summary>
        /// Applies the computed cursor state to Unity's Cursor system.
        /// Only makes Unity calls if the state actually changed.
        /// </summary>
        private static void ApplyCursorState()
        {
            bool shouldBeVisible = computedCursorVisible;
            CursorLockMode targetLockState = shouldBeVisible ? CursorLockMode.None : computedHiddenLockState;

            // Only apply if state changed
            if (shouldBeVisible != lastAppliedCursorVisible || targetLockState != lastAppliedCursorLockState)
            {
                Cursor.visible = shouldBeVisible;
                Cursor.lockState = targetLockState;

                lastAppliedCursorVisible = shouldBeVisible;
                lastAppliedCursorLockState = targetLockState;
            }
        }

        private static bool GetDefaultCursorVisibility(CursorVisibilityDefaultMode mode)
        {
            switch (mode)
            {
                case CursorVisibilityDefaultMode.InvisibleUnlessModified:
                    return false;
                case CursorVisibilityDefaultMode.OnlyVisibleOnAnyMenu:
                    return IsAnyMenuVisible();
                default:
                    return true;
            }
        }

        private static bool IsAnyMenuVisible()
        {
            var registry = RuntimeValues.TrackedValues.ViewRegistry;
            for (int i = 0; i < registry.Count; i++)
            {
                var view = registry[i];
                if (view != null && view.IsShown)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Forces a modifier reprocess on the next ProcessModifiers() call.
        /// </summary>
        public static void MarkModifiersDirty()
        {
            modifiersDirty = true;
        }

        #endregion

        public static void RefreshActionMaps()
        {
            if (!IsInputSystemEnabled())
            {
                ActionTypes.Clear();
                ActionToMapNames.Clear();
                KnownMapNames.Clear();
                MapToBlockedMapNames.Clear();
                actionMapsCached = true;
                mapSuppressionDirty = true;
                SuppressedMapsCache.Clear();
                return;
            }

            ActionTypes.Clear();
            ActionToMapNames.Clear();
            KnownMapNames.Clear();
            MapToBlockedMapNames.Clear();
            actionMapsCached = true;
            mapSuppressionDirty = true;
            SuppressedMapsCache.Clear();

            var actionMaps = ResourceHandler.GetInputActionMaps();
            if (actionMaps == null || actionMaps.Length == 0)
            {
                // No action maps, clear all states for actions that don't exist
                var actionsToRemove = new List<string>();
                foreach (var key in InputStates.Keys)
                {
                    actionsToRemove.Add(key);
                }
                foreach (var key in actionsToRemove)
                {
                    InputStates.Remove(key);
                }
                return;
            }

            var validActionNames = new HashSet<string>();
            foreach (var map in actionMaps)
            {
                if (map == null)
                {
                    continue;
                }

                string mapName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                KnownMapNames.Add(mapName);
                
                // Cache action map suppression rules
                if (map.BlockedActionMaps != null && map.BlockedActionMaps.Count > 0)
                {
                    if (!MapToBlockedMapNames.TryGetValue(mapName, out var blockedNames))
                    {
                        blockedNames = new HashSet<string>();
                        MapToBlockedMapNames[mapName] = blockedNames;
                    }

                    foreach (var blockedMap in map.BlockedActionMaps)
                    {
                        if (blockedMap == null)
                        {
                            continue;
                        }

                        string blockedName = string.IsNullOrWhiteSpace(blockedMap.MapName) ? blockedMap.name : blockedMap.MapName;
                        if (!string.IsNullOrWhiteSpace(blockedName))
                        {
                            blockedNames.Add(blockedName);
                        }
                    }
                }

                if (map.Actions == null)
                {
                    continue;
                }

                foreach (var action in map.Actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.ActionName))
                    {
                        continue;
                    }

                    string actionName = action.ActionName;
                    ActionTypes[actionName] = action.ActionType;
                    
                    // Track all maps that contain this action
                    if (!ActionToMapNames.TryGetValue(actionName, out var mapNames))
                    {
                        mapNames = new HashSet<string>();
                        ActionToMapNames[actionName] = mapNames;
                    }
                    mapNames.Add(mapName);
                    
                    validActionNames.Add(actionName);
                }
            }

            // Remove states for actions that no longer exist in the action map
            var statesToRemove = new List<string>();
            foreach (var key in InputStates.Keys)
            {
                if (!validActionNames.Contains(key))
                {
                    statesToRemove.Add(key);
                }
            }
            foreach (var key in statesToRemove)
            {
                InputStates.Remove(key);
            }

            // Apply initial map enable/disable state exactly once per runtime reset.
            // This ensures authoring defaults are respected on start, without overriding later runtime toggles.
            if (!initialActionMapStatesApplied)
            {
                ApplyInitialActionMapStates(actionMaps);
                initialActionMapStatesApplied = true;
            }
        }

        public static void DisableInput() => inputDisabled = true;

        public static void EnableInput() => inputDisabled = false;

        public static bool IsInputDisabled => inputDisabled;

        /// <summary>
        /// Directly disables an action.
        /// NOTE: Prefer using AddModifier/RemoveModifier for proper state management.
        /// Direct calls are overwritten when ProcessModifiers() runs.
        /// </summary>
        [Obsolete("Use AddModifier/SetModifier instead for proper state management. Direct disable/enable calls are overwritten by the modifier system.")]
        public static void DisableAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            DisabledActions.Add(actionName);
        }

        /// <summary>
        /// Directly enables an action.
        /// NOTE: Prefer using AddModifier/RemoveModifier for proper state management.
        /// Direct calls are overwritten when ProcessModifiers() runs.
        /// </summary>
        [Obsolete("Use AddModifier/SetModifier instead for proper state management. Direct disable/enable calls are overwritten by the modifier system.")]
        public static void EnableAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            DisabledActions.Remove(actionName);
        }

        /// <summary>
        /// Directly disables an action map. 
        /// NOTE: Prefer using AddModifier/RemoveModifier for proper state management.
        /// Direct calls are overwritten when ProcessModifiers() runs.
        /// </summary>
        [Obsolete("Use AddModifier/SetModifier instead for proper state management. Direct disable/enable calls are overwritten by the modifier system.")]
        public static void DisableActionMap(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return;
            }

            DisabledActionMaps.Add(mapName);
            mapSuppressionDirty = true;
        }

        /// <summary>
        /// Directly enables an action map.
        /// NOTE: Prefer using AddModifier/RemoveModifier for proper state management.
        /// Direct calls are overwritten when ProcessModifiers() runs.
        /// </summary>
        [Obsolete("Use AddModifier/SetModifier instead for proper state management. Direct disable/enable calls are overwritten by the modifier system.")]
        public static void EnableActionMap(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return;
            }

            DisabledActionMaps.Remove(mapName);
            mapSuppressionDirty = true;
        }

        /// <summary>
        /// Returns true if the action map is currently enabled (i.e., not disabled, and not suppressed by another enabled action map).
        /// </summary>
        public static bool IsActionMapEnabled(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return false;
            }

            if (!actionMapsCached)
            {
                RefreshActionMaps();
            }

            if (!KnownMapNames.Contains(mapName))
            {
                return false;
            }

            if (DisabledActionMaps.Contains(mapName))
            {
                return false;
            }

            return !GetSuppressedMaps().Contains(mapName);
        }

        /// <summary>
        /// Returns true if the action is currently enabled (i.e., action exists, is not disabled, and belongs to at least one enabled action map).
        /// Note: this does NOT include cooldown gating (use IsOnCooldown for that).
        /// </summary>
        public static bool IsActionEnabled(string actionName)
        {
            if (inputDisabled)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            if (!SignaliaInputWrapper.Exists)
            {
                return false;
            }

            if (!actionMapsCached)
            {
                RefreshActionMaps();
            }

            if (!ActionTypes.ContainsKey(actionName))
            {
                return false;
            }

            if (DisabledActions.Contains(actionName))
            {
                return false;
            }

            if (ActionToMapNames.TryGetValue(actionName, out var mapNames))
            {
                foreach (var mapName in mapNames)
                {
                    if (IsActionMapEnabled(mapName))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        public static void KillAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            if (InputStates.TryGetValue(actionName, out var state))
            {
                state.Kill();
            }
        }

        public static void KillAllActions()
        {
            foreach (var state in InputStates.Values)
            {
                state.Kill();
            }
        }

        public static void SetCooldown(string actionName, float duration, bool unscaled = false)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            if (duration <= 0f)
            {
                Cooldowns.Remove(actionName);
                return;
            }

            float endTime = (unscaled ? Time.unscaledTime : Time.time) + duration;
            Cooldowns[actionName] = new CooldownState(endTime, unscaled);
        }

        public static bool IsOnCooldown(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            if (!Cooldowns.TryGetValue(actionName, out var cooldown))
            {
                return false;
            }

            float current = cooldown.Unscaled ? Time.unscaledTime : Time.time;
            if (current >= cooldown.EndTime)
            {
                Cooldowns.Remove(actionName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Buffers an input action for later execution. The buffered input will expire after the specified duration.
        /// Use ConsumeBufferedInput() to check and consume a buffered input.
        /// </summary>
        /// <param name="actionName">The name of the action to buffer</param>
        /// <param name="duration">How long the buffer should last (in seconds). If null, uses the default from config.</param>
        /// <param name="unscaled">Whether to use unscaled time. If null, uses the setting from config.</param>
        public static void BufferInput(string actionName, float? duration = null, bool? unscaled = null)
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var config = ConfigReader.GetConfig();
            if (config == null || !config.InputSystem.EnableInputBuffering)
            {
                return;
            }

            float bufferDuration = duration ?? config.InputSystem.DefaultBufferDuration;
            bool useUnscaled = unscaled ?? config.InputSystem.UseUnscaledTimeForBuffers;

            if (bufferDuration <= 0f)
            {
                return;
            }

            float endTime = (useUnscaled ? Time.unscaledTime : Time.time) + bufferDuration;
            BufferedInputs[actionName] = new BufferedInputState(endTime, useUnscaled);
        }

        /// <summary>
        /// Checks if a buffered input exists and is still valid. If valid, consumes it (removes it from the buffer).
        /// </summary>
        /// <param name="actionName">The name of the action to check</param>
        /// <returns>True if a valid buffered input was found and consumed, false otherwise</returns>
        public static bool ConsumeBufferedInput(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            var config = ConfigReader.GetConfig();
            if (config == null || !config.InputSystem.EnableInputBuffering)
            {
                return false;
            }

            if (!BufferedInputs.TryGetValue(actionName, out var buffered))
            {
                return false;
            }

            float current = buffered.Unscaled ? Time.unscaledTime : Time.time;
            if (current >= buffered.EndTime)
            {
                BufferedInputs.Remove(actionName);
                return false;
            }

            // Valid buffer found - consume it
            BufferedInputs.Remove(actionName);
            return true;
        }

        /// <summary>
        /// Checks if a buffered input exists and is still valid without consuming it.
        /// </summary>
        /// <param name="actionName">The name of the action to check</param>
        /// <returns>True if a valid buffered input exists, false otherwise</returns>
        public static bool HasBufferedInput(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            var config = ConfigReader.GetConfig();
            if (config == null || !config.InputSystem.EnableInputBuffering)
            {
                return false;
            }

            if (!BufferedInputs.TryGetValue(actionName, out var buffered))
            {
                return false;
            }

            float current = buffered.Unscaled ? Time.unscaledTime : Time.time;
            if (current >= buffered.EndTime)
            {
                BufferedInputs.Remove(actionName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clears a specific buffered input, or all buffered inputs if actionName is null/empty.
        /// </summary>
        /// <param name="actionName">The action to clear, or null/empty to clear all buffers</param>
        public static void ClearBufferedInput(string actionName = null)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                BufferedInputs.Clear();
            }
            else
            {
                BufferedInputs.Remove(actionName);
            }
        }

        public static void PassDown(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var state = GetOrCreateState(actionName);
            if (state == null)
            {
                return;
            }

            state.ApplyDown(Time.frameCount);
        }

        public static void PassHeld(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var state = GetOrCreateState(actionName);
            if (state == null)
            {
                return;
            }

            state.ApplyHeld();
        }

        public static void PassUp(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var state = GetOrCreateState(actionName);
            if (state == null)
            {
                return;
            }

            state.ApplyUp(Time.frameCount);
        }

        /// <summary>Passes a float value to Signalia, updating the current recorded value.</summary>
        public static void PassFloat(string actionName, float value)
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var state = GetOrCreateState(actionName);
            if (state == null)
            {
                return;
            }

            state.ApplyHeld(value);
        }

        /// <summary>Passes a Vector2 value to Signalia, updating the current recorded value.</summary>
        public static void PassVector(string actionName, Vector2 value)
        {
            if (!IsInputSystemEnabled())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var state = GetOrCreateState(actionName);
            if (state == null)
            {
                return;
            }

            state.ApplyHeld(value);
        }

        public static bool Get(string actionName)
        {
            if (!CanRead(actionName))
            {
                return false;
            }

            return InputStates.TryGetValue(actionName, out var state) && state.Held;
        }

        public static bool GetDown(string actionName, bool oneFrame = false)
        {
            if (!CanRead(actionName))
            {
                return false;
            }

            if (!InputStates.TryGetValue(actionName, out var state))
            {
                return false;
            }

            bool isDown = state.DownFrame == Time.frameCount;
            if (!isDown)
            {
                return false;
            }

            if (oneFrame && state.DownConsumed)
            {
                return false;
            }

            if (oneFrame)
            {
                state.DownConsumed = true;
            }

            return true;
        }

        public static bool GetUp(string actionName, bool oneFrame = false)
        {
            if (!CanRead(actionName))
            {
                return false;
            }

            if (!InputStates.TryGetValue(actionName, out var state))
            {
                return false;
            }

            bool isUp = state.UpFrame == Time.frameCount;
            if (!isUp)
            {
                return false;
            }

            if (oneFrame && state.UpConsumed)
            {
                return false;
            }

            if (oneFrame)
            {
                state.UpConsumed = true;
            }

            return true;
        }

        public static float GetFloat(string actionName)
        {
            if (!CanRead(actionName))
            {
                return 0f;
            }

            return InputStates.TryGetValue(actionName, out var state) ? state.FloatValue : 0f;
        }

        public static Vector2 GetVector2(string actionName)
        {
            if (!CanRead(actionName))
            {
                return Vector2.zero;
            }

            return InputStates.TryGetValue(actionName, out var state) ? state.Vector2Value : Vector2.zero;
        }

        private static bool CanRead(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return false;
            }

            if (inputDisabled)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            if (DisabledActions.Contains(actionName))
            {
                return false;
            }
            
            // check if there is at least one wrapper that can read this action
            if (!SignaliaInputWrapper.Exists)
            {
                Debug.LogWarning("No SignaliaInputWrapper found. Cannot read input.");
                return false;
            }

            // Ensure action maps are cached
            if (!actionMapsCached)
            {
                RefreshActionMaps();
            }

            // Check if the action exists in the action map
            if (!ActionTypes.ContainsKey(actionName))
            {
                return false;
            }

            // Check if the action exists in at least one enabled action map
            // If an action is in multiple maps, it should pass if ANY of those maps is enabled
            if (ActionToMapNames.TryGetValue(actionName, out var mapNames))
            {
                bool hasEnabledMap = false;
                foreach (var mapName in mapNames)
                {
                    if (IsActionMapEnabled(mapName))
                    {
                        hasEnabledMap = true;
                        break;
                    }
                }
                
                if (!hasEnabledMap)
                {
                    // All maps containing this action are disabled
                    return false;
                }
            }

            return !IsOnCooldown(actionName);
        }

        private static HashSet<string> GetSuppressedMaps()
        {
            if (!mapSuppressionDirty)
            {
                return SuppressedMapsCache;
            }

            SuppressedMapsCache.Clear();

            foreach (var kvp in MapToBlockedMapNames)
            {
                string blockerMapName = kvp.Key;
                if (DisabledActionMaps.Contains(blockerMapName))
                {
                    continue; // only enabled maps suppress other maps
                }

                var blockedNames = kvp.Value;
                if (blockedNames == null || blockedNames.Count == 0)
                {
                    continue;
                }

                foreach (var blockedName in blockedNames)
                {
                    if (string.IsNullOrWhiteSpace(blockedName))
                    {
                        continue;
                    }

                    // Ignore self-suppression (authoring mistake)
                    if (blockedName == blockerMapName)
                    {
                        continue;
                    }

                    SuppressedMapsCache.Add(blockedName);
                }
            }

            mapSuppressionDirty = false;
            return SuppressedMapsCache;
        }

        private static void ApplyInitialActionMapStates(SignaliaActionMap[] actionMaps)
        {
            if (actionMaps == null || actionMaps.Length == 0)
            {
                return;
            }

            foreach (var map in actionMaps)
            {
                if (map == null)
                {
                    continue;
                }

                if (map.InitialState != SignaliaActionMapInitialState.Disabled)
                {
                    continue;
                }

                string mapName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                if (string.IsNullOrWhiteSpace(mapName))
                {
                    continue;
                }

                // Store in BaseDisabledActionMaps so it's preserved across modifier processing
                BaseDisabledActionMaps.Add(mapName);
                DisabledActionMaps.Add(mapName);
            }

            mapSuppressionDirty = true;
            modifiersDirty = true; // Ensure modifiers are reprocessed to include base state
        }

        private static SignaliaInputState GetOrCreateState(string actionName)
        {
            if (!IsInputSystemEnabled())
            {
                return null;
            }

            if (!InputStates.TryGetValue(actionName, out var state))
            {
                if (!actionMapsCached)
                {
                    RefreshActionMaps();
                }

                // Only create state if the action exists in the action map
                if (!ActionTypes.TryGetValue(actionName, out var foundType))
                {
                    // Action doesn't exist in action map, return null state
                    // This prevents creating states for actions that aren't defined
                    return null;
                }

                state = new SignaliaInputState(foundType);
                InputStates[actionName] = state;
            }

            return state;
        }

        private readonly struct CooldownState
        {
            public CooldownState(float endTime, bool unscaled)
            {
                EndTime = endTime;
                Unscaled = unscaled;
            }

            public float EndTime { get; }
            public bool Unscaled { get; }
        }

        private readonly struct BufferedInputState
        {
            public BufferedInputState(float endTime, bool unscaled)
            {
                EndTime = endTime;
                Unscaled = unscaled;
            }

            public float EndTime { get; }
            public bool Unscaled { get; }
        }

        private sealed class SignaliaInputState
        {
            public SignaliaInputState(SignaliaActionType actionType)
            {
                ActionType = actionType;
            }

            public SignaliaActionType ActionType { get; }
            public bool Held { get; private set; }
            public int DownFrame { get; private set; } = -1;
            public int UpFrame { get; private set; } = -1;
            public bool DownConsumed { get; set; }
            public bool UpConsumed { get; set; }
            public float FloatValue { get; private set; }
            public Vector2 Vector2Value { get; private set; }

            public void ApplyDown(int frame)
            {
                Held = true;
                DownFrame = frame;
                DownConsumed = false;
            }

            public void ApplyDown(int frame, float value)
            {
                FloatValue = value;
                ApplyDown(frame);
            }

            public void ApplyDown(int frame, Vector2 value)
            {
                Vector2Value = value;
                ApplyDown(frame);
            }

            public void ApplyHeld()
            {
                Held = true;
            }

            public void ApplyHeld(float value)
            {
                FloatValue = value;
                Held = true;
            }

            public void ApplyHeld(Vector2 value)
            {
                Vector2Value = value;
                Held = true;
            }

            public void ApplyUp(int frame)
            {
                Held = false;
                UpFrame = frame;
                UpConsumed = false;
            }

            public void ApplyUp(int frame, float value)
            {
                FloatValue = value;
                ApplyUp(frame);
            }

            public void ApplyUp(int frame, Vector2 value)
            {
                Vector2Value = value;
                ApplyUp(frame);
            }

            public void Kill()
            {
                Held = false;
                DownFrame = -1;
                UpFrame = -1;
                DownConsumed = false;
                UpConsumed = false;
            }
        }
    }
}
